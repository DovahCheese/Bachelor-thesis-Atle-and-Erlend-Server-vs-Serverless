using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace AzureFunctions.Middleware;

/// <summary>
/// Adds CORS response headers to every HTTP-triggered function.
/// Handles preflight OPTIONS requests inline so they never reach the function handler.
/// Mirrors the CORS policy configured in WebApi's Program.cs via Cors:AllowedOrigins.
/// </summary>
public class CorsMiddleware(IConfiguration config) : IFunctionsWorkerMiddleware
{
    private readonly string[] _allowedOrigins =
        config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext is not null && _allowedOrigins.Length > 0)
        {
            var origin = httpContext.Request.Headers.Origin.ToString();

            if (origin.Length > 0)
            {
                var matched = _allowedOrigins.Any(o =>
                    o.Equals("*", StringComparison.Ordinal) ||
                    o.Equals(origin, StringComparison.OrdinalIgnoreCase));

                if (matched)
                {
                    httpContext.Response.Headers.Append("Access-Control-Allow-Origin", origin);
                    httpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    httpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    httpContext.Response.Headers.Append("Access-Control-Max-Age", "86400");
                }
            }

            if (httpContext.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                httpContext.Response.StatusCode = 204;
                return;
            }
        }

        await next(context);
    }
}
