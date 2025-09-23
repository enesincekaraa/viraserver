namespace Vira.Application.Abstractions.Auth;
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Role { get; }
    bool IsAdmin { get; }
}
