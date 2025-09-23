using Vira.Shared.Base;

namespace Vira.Domain.Entities;
public class User : AuditableEntity<Guid>
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string? FullName { get; private set; } = default!;
    public string Role { get; private set; } = "User";

    private User() { }

    public User(string email, string passwordHash, string? fullName = null, string role = "User")
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newHash)
    {
        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }
}
