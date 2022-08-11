using Microsoft.AspNetCore.Mvc;
using ViberFitBot.ViberApi.Services;
using ViberFitBot.ViberApi.ViberModels;
using ViberFitBot.WebApi.Infrastructure;

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
    [ServiceFilter(typeof(ViberSignatureValidationFilter))]
    public async Task<IActionResult> RecieveCallback(Callback request)
    {
        _logger.LogInformation("Got request with event of type: {type}", request.Event);

        switch (request.Event)
        {
            case "subscribed":
            case "conversation_started":
                {
                    Request.Body.Seek(0, SeekOrigin.Begin);
                    var callback = (await HttpContext.Request.ReadFromJsonAsync<ConversationStartedCallback>())!;

                    Response.OnCompleted(() => _service.HandleConversationStartedCallback(callback));
                    break;
                }
            case "message":
                {
                    Request.Body.Seek(0, SeekOrigin.Begin);
                    var callback = (await HttpContext.Request.ReadFromJsonAsync<MessageCallback>())!;

                    Response.OnCompleted(() => _service.HandleMessageCallback(callback));
                    break;
                }
        }

        var token = _configuration.GetValue<string>("Viber:Token");
        Response.Headers.Add("X-Viber-Auth-Token", token);

        return Ok();
    }

    private readonly IConfiguration _configuration;
    private readonly ILogger<ViberWebhookController> _logger;
    private readonly ViberApiService _service;
}