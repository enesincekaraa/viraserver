using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Requests.Comments;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Comments;
public sealed record ListCommentQuery(Guid RequestId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<CommentResponse>>>;

public sealed class ListCommentHandler(IReadRepository<Request> _req, IReadRepository<RequestComment> _read)
    : IRequestHandler<ListCommentQuery, Result<PagedResult<CommentResponse>>>
{
    public async Task<Result<PagedResult<CommentResponse>>> Handle(ListCommentQuery request, CancellationToken cancellationToken)
    {
        var req = await _req.GetByIdAsync(request.RequestId, cancellationToken);
        if (req is null)
            return Result<PagedResult<CommentResponse>>.Failure("NotFound", "Request not found");
        if (req.IsDeleted == true)
            return Result<PagedResult<CommentResponse>>.Failure("Already deleted", "Silinmiş");
        var (items, total) = await _read.ListPagedAsync(
            request.Page, request.PageSize,
            x => x.RequestId == request.RequestId,/* && /*!x.IsDeleted*/
            x => x.OrderByDescending(c => c.CreatedAt),
            cancellationToken);


        var responses = items.Select(x => new CommentResponse(
            x.Id,
            x.RequestId,
            x.AuthorUserId,
            (int)x.Type,
            x.Text,
            x.CreatedAt
        )).ToList();
        var pagedResult = new PagedResult<CommentResponse>(responses, total, request.Page, request.PageSize);
        return Result<PagedResult<CommentResponse>>.Success(pagedResult);
    }
}
