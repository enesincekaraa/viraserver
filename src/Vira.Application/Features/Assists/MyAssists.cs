using MediatR;
using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Persistence;
using Vira.Contracts.Assists;
using Vira.Shared;

namespace Vira.Application.Features.Assists;
public sealed class MyAssists
{
    public sealed record MyAssistsListQuery(int Page = 1, int PageSize = 20)
       : IRequest<PagedResult<AssistsDtos.AssistListItem>>;

    public sealed class MyAssistsListHandler
        : IRequestHandler<MyAssistsListQuery, PagedResult<AssistsDtos.AssistListItem>>
    {
        private readonly IReadDb _db;
        private readonly ICurrentUser _me;

        public MyAssistsListHandler(IReadDb db, ICurrentUser me) { _db = db; _me = me; }

        public async Task<PagedResult<AssistsDtos.AssistListItem>> Handle(MyAssistsListQuery q, CancellationToken ct)
        {
            var uid = _me.UserId ?? throw new InvalidOperationException("User not authenticated");

            var s = _db.AssistTickets.AsNoTracking().Where(x => x.CreatedByUserId == uid);

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
