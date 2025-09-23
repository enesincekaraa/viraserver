using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Vira.Application.Features.Auth;
using Vira.Contracts.Auth;

namespace Vira.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    public AuthController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(new RegisterCommand(req.Email, req.Password, req.FullName), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(new LoginCommand(req.Email, req.Password), ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(result.Error);
    }

    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(new RefreshCommand(req.RefreshToken), ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(result.Error);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(ClaimTypes.Name); // fallback
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, out var userId))
        {
            // Sub claim guid değilse JWT oluştururken NameIdentifier eklemeyi düşünebilirsin.
            return Unauthorized();
        }
        var result = await _sender.Send(new MeQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(result.Error);
    }
}
