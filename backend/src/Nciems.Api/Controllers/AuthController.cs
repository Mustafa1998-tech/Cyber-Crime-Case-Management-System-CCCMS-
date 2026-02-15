using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nciems.Application.Features.Auth;

namespace Nciems.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<long>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var userId = await mediator.Send(new RegisterUserCommand(
            request.UserName,
            request.Email,
            request.Password,
            request.MfaEnabled,
            request.Roles), cancellationToken);

        return Ok(userId);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(request.UserName, request.Password, request.DeviceInfo), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("verify-mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new VerifyMfaCommand(request.UserName, request.OtpCode), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }
}

public sealed class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool MfaEnabled { get; set; } = true;
    public string[]? Roles { get; set; }
}

public sealed class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = "Unknown";
}

public sealed class VerifyMfaRequest
{
    public string UserName { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
