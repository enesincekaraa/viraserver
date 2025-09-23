using MediatR;
using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Persistence;
using Vira.Application.Features.Requests.AdminList;
using Vira.Shared;

public sealed class AdminListHandler
    : IRequestHandler<AdminListQuery, PagedResult<RequestListItemDto>>
{
    private readonly IReadDb _db;
    public AdminListHandler(IReadDb db) => _db = db;

    public async Task<PagedResult<RequestListItemDto>> Handle(AdminListQuery q, CancellationToken ct)
    {
        var s = _db.Requests.AsNoTracking().Where(x => !x.IsDeleted);

        if (q.Status.HasValue) s = s.Where(x => (int)x.Status == q.Status.Value);
        if (q.CategoryId.HasValue) s = s.Where(x => x.CategoryId == q.CategoryId);
        if (q.CreatedByUserId.HasValue) s = s.Where(x => x.CreatedByUserId == q.CreatedByUserId);
        if (q.FromUtc.HasValue) s = s.Where(x => x.CreatedAt >= q.FromUtc.Value);
        if (q.ToUtc.HasValue) s = s.Where(x => x.CreatedAt <= q.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            s = s.Where(x => x.Title.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }

        var total = await s.CountAsync(ct);

        var items = await s
            .OrderByDescending(x => x.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(x => new RequestListItemDto(
                x.Id, x.Title, x.Description, x.CategoryId,
                (int)x.Status, x.CreatedByUserId, x.AssignedToUserId,
                x.Latitude, x.Longitude, x.CreatedAt))
            .ToListAsync(ct);

        return PagedResultCreate<RequestListItemDto>.Create(items, q.Page, q.PageSize, total);
    }
}
