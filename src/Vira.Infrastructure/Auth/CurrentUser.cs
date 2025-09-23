using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Vira.Application.Abstractions.Auth;

namespace Vira.Infrastructure.Auth;
public sealed class CurrentUser(IHttpContextAccessor _http) : ICurrentUser
{

    public Guid? UserId
    {
        get
        {
            var sub = _http.HttpContext?.User.FindFirst("sub")?.Value
                   ?? _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : (Guid?)null;
        }
    }

    public string? Role => _http.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
}
