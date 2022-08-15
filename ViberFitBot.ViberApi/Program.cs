using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using ViberFitBot.ViberApi;
using ViberFitBot.ViberApi.Infrastructure;
using ViberFitBot.ViberApi.Models;
using ViberFitBot.ViberApi.Services;
using ViberFitBot.ViberApi.ViberModels;

var builder = WebApplication.CreateBuilder(args);

ConfigureApplicationBuilder(builder);

var app = builder.Build();

await ConfigureApplication(app);

app.Run();

static void ConfigureApplicationBuilder(WebApplicationBuilder builder)
{
    if (builder.Environment.IsProduction())
    {
        builder.Configuration.AddAzureKeyVault(new Uri(builder.Configuration.GetValue<string>("KeyVault")), new DefaultAzureCredential());
    }

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddLogging(o =>
    {
        o.AddConsole();
        o.AddDebug();
    });

    builder.Services.AddSingleton<ViberApiHttpClient>(services => new(builder.Configuration, services.GetRequiredService<ILogger<ViberApiHttpClient>>()));
    builder.Services.AddScoped<ViberApiService>();
    builder.Services.AddScoped<ITrackService, TrackServiceWithLinqToEntities>();

    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    {
        p.WithOrigins(builder.Configuration.GetValue<string>("Viber:Origin"));
        p.WithMethods("POST");
        p.AllowAnyHeader();
        p.DisallowCredentials();
        p.SetPreflightMaxAge(TimeSpan.FromMinutes(5));
    }));

    builder.Services.AddDbContextPool<TrackContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Database")));
}

static async Task ConfigureApplication(WebApplication app)
{
    app.Use((context, next) =>
    {
        // To enable reading HttpRequest as string in ViberSignatureVaildationFilter
        context.Request.EnableBuffering();
        return next();
    });

    app.UseSwagger();
    app.UseSwaggerUI();

    // app.UseCors();

    app.MapControllers();

    using var scope = app.Services.CreateScope();
    await CheckTracksTableSeeded(scope.ServiceProvider.GetRequiredService<TrackContext>());

    if (app.Environment.IsProduction())
    {
        try
        {
            await SetupViberWebhook(app.Configuration, app.Services.GetRequiredService<ViberApiHttpClient>(), app.Logger);
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical("Unable to setup webhook with viber because of: {ex} {inner}", ex.Message, ex.InnerException?.Message ?? string.Empty);
        }
    }
}

static async Task CheckTracksTableSeeded(TrackContext ctx)
{
    var isSeeded = await ctx.Tracks.AnyAsync();

    if (!isSeeded)
    {
        var tracks = new HashSet<Track>();

        var data = await ctx.TrackLocations
            .OrderBy(tl => tl.DateTrack)
            .ToListAsync();

        foreach (var tl in data)
        {
            var track = tracks
                .Where(t => t.Imei == tl.Imei)
                .OrderBy(t => t.StartTimeUtc)
                .LastOrDefault();

            if (track != null && track.LatestData.DateTrack.AddMinutes(TrackServiceWithLinqToEntities.CreateTrackWhenTimePassedMinutes) >= tl.DateTrack)
            {
                // If track exists and time passed <= 30 mins: update latest track data of the user
                track.Duration = tl.DateTrack - track.FirstData.DateTrack;
                track.DistanceMetres += TrackServiceWithLinqToEntities.GetDistanceBetweenPoints(track.LatestData, tl);

                track.LatestData = tl;
            }
            else
            {
                // If latest track has zero duration (and zero distance as well) - delete it.
                if (track?.Duration == TimeSpan.Zero || track?.DistanceMetres == 0) tracks.Remove(track);

                // If track doesn't exist or time passed > 30 mins: create new track
                track = new()
                {
                    Imei = tl.Imei,
                    StartTimeUtc = tl.DateTrack,
                    FirstData = tl,
                    LatestData = tl
                };

                tracks.Add(track);
            }
        }

        ctx.Tracks.AddRange(tracks);
        await ctx.SaveChangesAsync();
    }
}

static async Task SetupViberWebhook(IConfiguration configuration, ViberApiHttpClient httpClient, ILogger logger)
{
    var setWebhookUrl = configuration.GetValue<string>("Viber:SetWebhook");
    var apiUrl = configuration.GetValue<string>("ApiUrl");

    var responseJson = await httpClient.PostAsJsonAsync(setWebhookUrl, new
    {
        url = apiUrl,
        event_types = new[] { "conversation_started", "subscribed" }
    });

    var response = await responseJson.Content.ReadFromJsonAsync<ViberActionResult>();
    if (response?.Status != 0) logger.LogError("Failed to send message, status: {status} and message: {message}", response?.Status, response?.StatusMessage);
}