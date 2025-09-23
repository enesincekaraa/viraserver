using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Requests;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;
public sealed record ListAttachmentQuery(Guid RequestId) : IRequest<Result<List<AttachmentResponse>>>;

public sealed class ListAttachmentHandler(IReadRepository<RequestAttachment> _read, IReadRepository<Request> _req) : IRequestHandler<ListAttachmentQuery, Result<List<AttachmentResponse>>>
{
    public async Task<Result<List<AttachmentResponse>>> Handle(ListAttachmentQuery request, CancellationToken cancellationToken)
    {
        var req = await _req.GetByIdAsync(request.RequestId, cancellationToken);
        if (req is null) return Result<List<AttachmentResponse>>.Failure("Request.NotFound", "Talep bulunamadı.");
        var (items, _) = await _read.ListPagedAsync(1, int.MaxValue,
             predicate: a => a.RequestId == request.RequestId,
             orderBy: s => s.OrderByDescending(x => x.CreatedAt),
             ct: cancellationToken);

        var list = items.Select(a => new AttachmentResponse(
            a.Id, a.OriginalName, a.Url, a.ContentType, a.SizeBytes, a.CreatedAt)).ToList();

        return Result<List<AttachmentResponse>>.Success(list);
    }
}



