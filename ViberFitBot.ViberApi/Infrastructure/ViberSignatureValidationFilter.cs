using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ViberFitBot.WebApi.Infrastructure;

public class ViberSignatureValidationFilter : ActionFilterAttribute
{
    public ViberSignatureValidationFilter(IConfiguration configuration)
    {
        token = configuration.GetValue<string>("Viber:Token");
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {

        var request = context.HttpContext.Request;

        // Check signature header exists
        if (!request.Headers.TryGetValue("X-Viber-Content-Signature", out var signatureHex))
        {
            context.Result = new StatusCodeResult(200);
            return;
        }
        var signature = Convert.FromHexString(signatureHex.First());

        // Get body and hash it
        string body = await GetBodyAsStringAsync(request);

        var hasher = new HMACSHA256(Encoding.ASCII.GetBytes(token));
        var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(body));

        // Compare computed with stored in header
        if (!signature.SequenceEqual(hash))
        {
            context.Result = new StatusCodeResult(200);
            return;
        }

        await next();
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

    private readonly string token;
}