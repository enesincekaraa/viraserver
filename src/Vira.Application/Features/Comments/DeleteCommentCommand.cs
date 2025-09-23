using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Comments;
public sealed record DeleteCommentCommand(Guid RequestId, Guid CommentId, Guid PerformedByUserId, bool IsAdminOrOperator)
    : IRequest<Result>;

public sealed class DeleteCommentHandler(IRepository<RequestComment> _comments, IReadRepository<Request> _req, IUnitOfWork _uow)
    : IRequestHandler<DeleteCommentCommand, Result>
{
    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var req = await _req.GetByIdAsync(request.RequestId, cancellationToken);
        if (req is null)
            return Result.Failure("Request not found", "Talep bulunamadı");
        var comment = await _comments.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null || comment.RequestId != request.RequestId)
            return Result.Failure("Comment not found", "Yorum bulunamadı");

        if (comment.AuthorUserId != request.PerformedByUserId && !request.IsAdminOrOperator)
            return Result.Failure("Not allowed", "Yorumu silmeye yetkiniz yok");

        if (comment.IsDeleted)
            return Result.Failure("Already deleted", "Yorum zaten silinmiş");
        comment.SoftDelete(request.PerformedByUserId);
        await _comments.UpdateAsync(comment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
