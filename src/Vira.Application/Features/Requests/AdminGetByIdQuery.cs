using MediatR;
using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Persistence;
using Vira.Contracts.Requests;

namespace Vira.Application.Features.Requests;
public sealed record AdminGetByIdQuery(Guid Id) : IRequest<AdminRequestDetailDto>;

public sealed record AdminRequestDetailDto(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    int Status,
    Guid CreatedByUserıd,
    Guid? AssignedUserId,
    double Latitude,
    double Longitude,
    DateTime CreatedAt,
    List<AttachmentResponse> Attachments,
    List<CommentDto> Comments
    );
public sealed record CommentDto(Guid Id, Guid AuthorUserId, int Type, string Text, DateTime CreatedAtUtc);

public sealed class AdminGetByIdHandler
    : IRequestHandler<AdminGetByIdQuery, AdminRequestDetailDto>
{
    private readonly IReadDb _db;
    public AdminGetByIdHandler(IReadDb db) => _db = db;

    public async Task<AdminRequestDetailDto> Handle(AdminGetByIdQuery q, CancellationToken ct)
    {
        var r = await _db.Requests.AsNoTracking()
            .Where(x => x.Id == q.Id && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.CategoryId,
                Status = (int)x.Status,
                x.CreatedByUserId,
                x.AssignedToUserId,
                x.Latitude,
                x.Longitude,
                x.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (r is null) return null;

        var attachments = await _db.RequestAttachments.AsNoTracking()
            .Where(a => a.RequestId == q.Id && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentResponse(
                a.Id, a.OriginalName, a.Url, a.ContentType, a.SizeBytes, a.CreatedAt))
            .ToListAsync(ct);

        var comments = await _db.RequestComments.AsNoTracking()
            .Where(c => c.RequestId == q.Id && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.AuthorUserId, (int)c.Type, c.Text, c.CreatedAt))
            .ToListAsync(ct);

        return new AdminRequestDetailDto(
            r.Id, r.Title, r.Description, (Guid)r.CategoryId, r.Status,
            r.CreatedByUserId, r.AssignedToUserId,
            r.Latitude, r.Longitude, r.CreatedAt,
            attachments, comments);
    }
}


