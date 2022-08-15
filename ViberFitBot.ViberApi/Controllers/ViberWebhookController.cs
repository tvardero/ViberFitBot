using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ViberFitBot.ViberApi.Resources;
using ViberFitBot.ViberApi.Services;
using ViberFitBot.ViberApi.ViberModels;

namespace ViberFitBot.WebApi.Controllers;

[ApiController, Route("")]
public class ViberWebhookController : ControllerBase
{
    public ViberWebhookController(IConfiguration configuration, ILogger<ViberWebhookController> logger, ViberApiService service)
    {
        _configuration = configuration;
        _logger = logger;
        _service = service;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> RecieveCallback(Callback request)
    {
        _logger.LogInformation("Got request with event of type: {type}", request.Event);

        switch (request.Event)
        {
            case "webhook":
                {
                    if (!await CheckSignatureAsync(Request)) return Ok();

                    var token = _configuration.GetValue<string>("Viber:Token");
                    Response.Headers.Add("X-Viber-Auth-Token", token);

                    return Ok();
                }
            case "subscribed" or "conversation_started":
                {
                    if (!await CheckSignatureAsync(Request)) return Ok();

                    Request.Body.Seek(0, SeekOrigin.Begin);
                    var callback = (await HttpContext.Request.ReadFromJsonAsync<ConversationStartedCallback>())!;

                    var token = _configuration.GetValue<string>("Viber:Token");
                    Response.Headers.Add("X-Viber-Auth-Token", token);

                    return Ok(new
                    {
                        sender = new { name = "ViberFitBot" },
                        type = "text",
                        text = Responses.WelcomeMessage,
                        keyboard = Keyboards.MainMenuKeyboard
                    });
                }
            case "message":
                {
                    if (!await CheckSignatureAsync(Request)) return Ok();

                    Request.Body.Seek(0, SeekOrigin.Begin);
                    var callback = (await HttpContext.Request.ReadFromJsonAsync<MessageCallback>())!;

                    Response.OnCompleted(() => _service.HandleMessageCallback(callback));

                    var token = _configuration.GetValue<string>("Viber:Token");
                    Response.Headers.Add("X-Viber-Auth-Token", token);

                    return Ok();
                }
        }

        return Ok();
    }

    public async Task<bool> CheckSignatureAsync(HttpRequest request)
    {
        // Check signature header exists
        if (!request.Headers.TryGetValue("X-Viber-Content-Signature", out var signatureHex))
        {
            return false;
        }
        var signature = Convert.FromHexString(signatureHex.First());

        // Get body and hash it
        string body = await GetBodyAsStringAsync(request);

        var token = _configuration.GetValue<string>("Viber:Token");
        var hasher = new HMACSHA256(Encoding.ASCII.GetBytes(token));
        var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(body));

        // Compare computed with stored in header
        if (!signature.SequenceEqual(hash))
        {
            return false;
        }

        return true;
    }

    // https://stackoverflow.com/questions/40494913/how-to-read-request-body-in-an-asp-net-core-webapi-controller
    private static async Task<string> GetBodyAsStringAsync(HttpRequest request)
    {
        request.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
        var content = await reader.ReadToEndAsync();

        request.Body.Seek(0, SeekOrigin.Begin);

        return content;
    }

    private readonly IConfiguration _configuration;
    private readonly ILogger<ViberWebhookController> _logger;
    private readonly ViberApiService _service;
}