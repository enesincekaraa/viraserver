using MediatR;
using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Persistence;

namespace Vira.Application.Features.Requests.Stats;

// DTO’lar
public sealed record RequestsStatsDto(
    int Total,
    Dictionary<int, int> ByStatus,
    List<CategoryCountItem> TopCategories,
    List<DailyCountItem> Last7Days
);

public sealed record CategoryCountItem(Guid CategoryId, int Count);
public sealed record DailyCountItem(DateOnly Day, int Count);

// Query
public sealed record RequestsStatsQuery : IRequest<RequestsStatsDto>;

// Handler
public sealed class RequestsStatsHandler : IRequestHandler<RequestsStatsQuery, RequestsStatsDto>
{
    private readonly IReadDb _db;
    public RequestsStatsHandler(IReadDb db) => _db = db;

    public async Task<RequestsStatsDto> Handle(RequestsStatsQuery _, CancellationToken ct)
    {
        // Soft-delete filtreli ana sorgu
        var baseQ = _db.Requests.Where(r => !r.IsDeleted);

        // 1) Toplam
        var total = await baseQ.CountAsync(ct);

        // 2) Duruma göre sayılar (int sözlük)
        var byStatusPairs = await baseQ
            .GroupBy(r => r.Status)
            .Select(g => new { Status = (int)g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byStatus = byStatusPairs.ToDictionary(x => x.Status, x => x.Count);

        // 3) En çok talep alan ilk 5 kategori
        // Not: EF çevirisini bozmasın diye anonime projekte edip sonra DTO'ya çeviriyoruz.
        var topCatsRaw = await baseQ
            .Where(r => r.CategoryId != null)
            .GroupBy(r => r.CategoryId)                   // Guid?
            .Select(g => new { CategoryId = g.Key!, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        var topCategories = topCatsRaw
            .Select(x => new CategoryCountItem(x.CategoryId.Value, x.Count))
            .ToList();

        // 4) Son 7 gün – PostgreSQL için DateTrunc kullan
        var endExclusive = DateTime.UtcNow.Date.AddDays(1);
        var startInclusive = endExclusive.AddDays(-7);

        var last7Raw = await baseQ
            .Where(r => r.CreatedAt >= startInclusive && r.CreatedAt < endExclusive)
            .GroupBy(r => r.CreatedAt.Date) // <-- provider bunu server-side’a çevirir
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderBy(x => x.Day)
            .ToListAsync(ct);

        var last7 = last7Raw
            .Select(x => new DailyCountItem(DateOnly.FromDateTime(x.Day), x.Count))
            .ToList();



        return new RequestsStatsDto(total, byStatus, topCategories, last7);
    }
}
