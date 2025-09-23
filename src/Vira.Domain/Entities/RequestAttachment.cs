using Vira.Shared.Base;

namespace Vira.Domain.Entities;
public class RequestAttachment : AuditableEntity<Guid>
{
    public Guid RequestId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string OriginalName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string Url { get; private set; } = default!;
    private RequestAttachment() { }
    public RequestAttachment(Guid requestId, string fileName, string originalName, string contentType, long sizeBytes, string url)
    {
        Id = Guid.NewGuid(); RequestId = requestId; FileName = fileName; OriginalName = originalName;
        ContentType = contentType; SizeBytes = sizeBytes; Url = url; CreatedAt = DateTime.UtcNow;
    }

}
