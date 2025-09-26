using MediatR;
using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Persistence;

namespace Vira.Application.Features.Assists;
public sealed class AdminAssistsStats
{


    public sealed record AssistStatsDto(
        int Total,
        Dictionary<int, int> ByStatus,
        List<TypeCountItem> TopTypes,
        List<DailyCountItem> Last7Days);

    public sealed record TypeCountItem(int Type, int Count);
    public sealed record DailyCountItem(DateOnly Day, int Count);

    public sealed record AdminAssistStatsQuery() : IRequest<AssistStatsDto>;

    public sealed class AdminAssistStatsHandler : IRequestHandler<AdminAssistStatsQuery, AssistStatsDto>
    {
        private readonly IReadDb _db;
        public AdminAssistStatsHandler(IReadDb db) => _db = db;

        public async Task<AssistStatsDto> Handle(AdminAssistStatsQuery _, CancellationToken ct)
        {
            var baseQ = _db.AssistTickets; // global filter: IsDeleted=false

            var total = await baseQ.CountAsync(ct);

            var byStatus = await baseQ
                .GroupBy(x => (int)x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            var topTypes = await baseQ
                .GroupBy(x => (int)x.Type)
                .Select(g => new TypeCountItem(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync(ct);

            var endExclusive = DateTime.UtcNow.Date.AddDays(1);
            var startInclusive = endExclusive.AddDays(-7);

            // Replace the following block in AdminAssistStatsHandler.Handle:

            var last7Raw = await baseQ
                .Where(r => r.CreatedAt >= startInclusive && r.CreatedAt < endExclusive)
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var days = Enumerable.Range(0, 7)
                .Select(i => DateOnly.FromDateTime(startInclusive.AddDays(i)))
                .ToList();

            var last7 = days.Select(d =>
            {
                var match = last7Raw.FirstOrDefault(x =>
                    DateOnly.FromDateTime(x.Day) == d);
                return new DailyCountItem(d, match?.Count ?? 0);
            }).ToList();

            return new AssistStatsDto(total, byStatus, topTypes, last7);
        }
    }

}
