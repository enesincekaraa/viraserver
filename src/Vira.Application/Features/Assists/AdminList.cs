using MediatR;
using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Persistence;
using Vira.Contracts.Assists;
using Vira.Shared;

namespace Vira.Application.Features.Assists;
public sealed class AdminList
{
    public sealed record AdminListQuery(
    int Page = 1, int PageSize = 20,
    int? Status = null, int? Type = null,
    Guid? CreatedByUserId = null, Guid? AssignedToUserId = null,
    DateTime? FromUtc = null, DateTime? ToUtc = null,
    string? Search = null)
    : IRequest<PagedResult<AssistsDtos.AssistListItem>>;

    public sealed class AdminListHandler
        : IRequestHandler<AdminListQuery, PagedResult<AssistsDtos.AssistListItem>>
    {
        private readonly IReadDb _db;
        public AdminListHandler(IReadDb db) => _db = db;

        public async Task<PagedResult<AssistsDtos.AssistListItem>> Handle(AdminListQuery q, CancellationToken ct)
        {
            var s = _db.AssistTickets.AsNoTracking();

            if (q.Status.HasValue) s = s.Where(x => (int)x.Status == q.Status);
            if (q.Type.HasValue) s = s.Where(x => (int)x.Type == q.Type);
            if (q.CreatedByUserId.HasValue) s = s.Where(x => x.CreatedByUserId == q.CreatedByUserId);
            if (q.AssignedToUserId.HasValue) s = s.Where(x => x.AssignedToUserId == q.AssignedToUserId);
            if (q.FromUtc.HasValue) s = s.Where(x => x.CreatedAt >= q.FromUtc);
            if (q.ToUtc.HasValue) s = s.Where(x => x.CreatedAt <= q.ToUtc);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var t = q.Search.Trim();
                s = s.Where(x => x.ElderFullName.Contains(t) || x.Address.Contains(t));
            }

            var total = await s.CountAsync(ct);
            var items = await s.OrderByDescending(x => x.CreatedAt)
                .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
                .Select(x => new AssistsDtos.AssistListItem(
                    x.Id, (int)x.Type, (int)x.Status, x.ElderFullName, x.Address, x.CreatedAt, x.AssignedToUserId))
                .ToListAsync(ct);

            return PagedResultCreate<AssistsDtos.AssistListItem>.Create(items, q.Page, q.PageSize, total);
        }
    }
}
