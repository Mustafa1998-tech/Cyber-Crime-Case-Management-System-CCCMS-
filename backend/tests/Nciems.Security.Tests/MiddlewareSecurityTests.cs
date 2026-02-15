using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Nciems.Api.Middleware;

namespace Nciems.Security.Tests;

public sealed class MiddlewareSecurityTests
{
    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldApply_HardeningHeaders()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new SecurityHeadersMiddleware(async _ =>
        {
            await context.Response.WriteAsync("ok");
        });

        await middleware.Invoke(context);
        await context.Response.StartAsync();

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"].ToString());
        Assert.Contains("default-src 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
        Assert.False(context.Response.Headers.ContainsKey("Server"));
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_ShouldHide_InternalServerErrorDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new Exception("Sensitive SQL details should not leak."),
            logger: new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExceptionHandlingMiddleware>());

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        var payload = await JsonDocument.ParseAsync(context.Response.Body);
        var root = payload.RootElement;

        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
    }
}
