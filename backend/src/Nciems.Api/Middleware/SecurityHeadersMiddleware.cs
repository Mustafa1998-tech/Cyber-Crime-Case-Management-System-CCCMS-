namespace Nciems.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ContentSecurityPolicy =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";

    public async Task Invoke(HttpContext context)
    {
        ApplyHeaders(context.Response.Headers);

        context.Response.OnStarting(() =>
        {
            ApplyHeaders(context.Response.Headers);
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static void ApplyHeaders(IHeaderDictionary headers)
    {
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Content-Security-Policy"] = ContentSecurityPolicy;
        headers.Remove("Server");
    }
}
