using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;

namespace CabinReservation.Api.Middleware;

public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // if the requested endpoint has the AllowAnonymous attribute, skip API key validation
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        var configuredKey = configuration["ApiSecurity:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey.StartsWith("CHANGE-ME", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "API security is not configured."
            });
            return;
        }

        // Validate the API key from the request header
        //if (!context.Request.Headers.TryGetValue(HeaderName, out var suppliedKey) ||
        //    !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
        //        System.Text.Encoding.UTF8.GetBytes(configuredKey),
        //        System.Text.Encoding.UTF8.GetBytes(suppliedKey.ToString())))
        //{
        //    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //    await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });
        //    return;
        //}

        await next(context);
    }
}
