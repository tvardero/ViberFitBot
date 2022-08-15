using ViberFitBot.ViberApi.ViberModels;

namespace ViberFitBot.ViberApi.Infrastructure;

public class ViberApiHttpClient : HttpClient
{

    public ViberApiHttpClient(IConfiguration configuration, ILogger<ViberApiHttpClient> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var token = configuration.GetValue<string>("Viber:Token");
        DefaultRequestHeaders.Add("X-Viber-Auth-Token", token);
    }

    public ViberApiHttpClient(IConfiguration configuration, ILogger<ViberApiHttpClient> logger, HttpMessageHandler handler) : base(handler)
    {
        _configuration = configuration;
        _logger = logger;

        var token = configuration.GetValue<string>("Viber:Token");
        DefaultRequestHeaders.Add("X-Viber-Auth-Token", token);
    }

    public ViberApiHttpClient(IConfiguration configuration, ILogger<ViberApiHttpClient> logger, HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
    {
        _configuration = configuration;
        _logger = logger;

        var token = configuration.GetValue<string>("Viber:Token");
        DefaultRequestHeaders.Add("X-Viber-Auth-Token", token);
    }

    public async Task SendTextMessageAsync(string userId, string text, InteractiveMedia? keyboard = null)
    {
        if (keyboard != null && keyboard.Type != "keyboard") throw new ArgumentException($"Unallowed type of keyboard: {keyboard.Type}", nameof(keyboard));

        var requestBody = new
        {
            receiver = userId,
            type = "text",
            text,
            sender = new { name = "ViberFitBot" },
            keyboard
        };

        await SendMessageAsync(requestBody);
    }

    public async Task SendRichMediaMessageAsync(string userId, InteractiveMedia media, InteractiveMedia? keyboard = null)
    {
        if (media.Type != "rich_media") throw new ArgumentException($"Unallowed type of rich media: {media.Type}", nameof(media));
        if (keyboard != null && keyboard.Type != "keyboard") throw new ArgumentException($"Unallowed type of keyboard: {keyboard.Type}", nameof(keyboard));

        var requestBody = new
        {
            receiver = userId,
            min_api_version = 7,
            type = "rich_media",
            rich_media = media,
            sender = new { name = "ViberFitBot" },
            keyboard
        };

        await SendMessageAsync(requestBody);
    }

    public async Task SendMessageAsync(object requestBody)
    {
        var sendMessageUrl = _configuration.GetValue<string>("Viber:SendMessage");

        var resultRaw = await this.PostAsJsonAsync(sendMessageUrl, requestBody, new System.Text.Json.JsonSerializerOptions() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

        var result = await resultRaw.Content.ReadFromJsonAsync<ViberActionResult>();
        if (result?.Status != 0)
        {
            _logger.LogError("Failed to send message, got status {status} and message {message}", result?.Status, result?.StatusMessage);
        }
    }

    private readonly IConfiguration _configuration;
    private readonly ILogger<ViberApiHttpClient> _logger;
}