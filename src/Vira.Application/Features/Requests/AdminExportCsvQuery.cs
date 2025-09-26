using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Vira.Application.Abstractions.Persistence;

namespace Vira.Application.Features.Requests;

public sealed record AdminExportCsvQuery(
    int? Status, Guid? CategoryId, Guid? CreatedByUserId,
    DateTime? FromUtc, DateTime? ToUtc, string? Search
) : IRequest<(string FileName, string Csv, string ContentType)>;

public sealed class AdminExportCsvHandler(IReadDb _db)
    : IRequestHandler<AdminExportCsvQuery, (string, string, string)>
{
    public async Task<(string, string, string)> Handle(AdminExportCsvQuery q, CancellationToken ct)
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
            s = s.Where(x => x.Title.Contains(term) || x.Description != null && x.Description.Contains(term));
        }

        var rows = await s.OrderByDescending(x => x.CreatedAt)
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
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("id,title,description,categoryId,status,createdByUserId,assignedToUserId,latitude,longitude,createdAtUtc");
        foreach (var r in rows)
        {
            static string Esc(string? v) => v is null ? "" : "\"" + v.Replace("\"", "\"\"") + "\"";
            sb.AppendLine(string.Join(',',
                r.Id, Esc(r.Title), Esc(r.Description),
                r.CategoryId, r.Status, r.CreatedByUserId, r.AssignedToUserId,
                r.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.CreatedAt.ToUniversalTime().ToString("O")));
        }

        return ($"requests_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv", sb.ToString(), "text/csv; charset=utf-8");
    }
}
