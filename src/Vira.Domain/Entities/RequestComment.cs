using Vira.Shared.Base;

namespace Vira.Domain.Entities;

public enum CommentType
{
    UserComment = 0,   // Kullanıcı yazdı
    SystemNote = 1   // Sistem olayı (assign/resolve gibi)
}

public class RequestComment : AuditableEntity<Guid>
{
    public Guid RequestId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public CommentType Type { get; private set; } = CommentType.UserComment;

    public string Text { get; private set; } = default!;
    public bool IsDeleted { get; private set; } = false;

    public RequestComment(Guid requestId, Guid authorUserId, string text, CommentType type = CommentType.UserComment)
    {
        Id = Guid.NewGuid();
        RequestId = requestId;
        AuthorUserId = authorUserId;
        Text = text;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    public void SoftDelete(Guid byUser)
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
