namespace Vira.Contracts.Requests.Comments;

public sealed record CommentResponse(
    Guid Id,
    Guid RequestId,
    Guid AuthorUserId,
    int Type,
    string Text,
    DateTime CreatedAtUtc
);
