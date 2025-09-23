using Vira.Shared.Base;

namespace Vira.Domain.Entities;
public class RefreshToken : AuditableEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool Revoked { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, string token, DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        Revoked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        Revoked = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
