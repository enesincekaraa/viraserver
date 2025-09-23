namespace Vira.Contracts.Requests;
public sealed record AttachmentResponse(
    Guid Id, string OriginalName, string Url, string ContentType, long SizeBytes, DateTime CreatedAtUtc);
