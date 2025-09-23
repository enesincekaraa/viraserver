using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Vira.Application.Abstractions.Persistence;

public sealed record AdminExportCsvQuery(
    int? Status = null,
    Guid? CategoryId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null
) : IRequest<byte[]>; // text/csv bytes


public sealed class AdminExportCsvHandler : IRequestHandler<AdminExportCsvQuery, byte[]>
{
    private readonly IReadDb _db;
    public AdminExportCsvHandler(IReadDb db) => _db = db;

    public async Task<byte[]> Handle(AdminExportCsvQuery q, CancellationToken ct)
    {
        var s = _db.Requests.AsNoTracking().Where(x => !x.IsDeleted);

        if (q.Status.HasValue) s = s.Where(x => (int)x.Status == q.Status.Value);
        if (q.CategoryId.HasValue) s = s.Where(x => x.CategoryId == q.CategoryId);
        if (q.FromUtc.HasValue) s = s.Where(x => x.CreatedAt >= q.FromUtc.Value);
        if (q.ToUtc.HasValue) s = s.Where(x => x.CreatedAt <= q.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            s = s.Where(x => x.Title.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }

        var rows = await s
            .OrderByDescending(x => x.CreatedAt)
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
        sb.AppendLine("Id,Title,Description,CategoryId,Status,CreatedByUserId,AssignedToUserId,Latitude,Longitude,CreatedAtUtc");
        foreach (var r in rows)
        {
            string qv(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";
            sb.AppendLine(string.Join(",",
                r.Id, qv(r.Title), qv(r.Description),
                r.CategoryId?.ToString() ?? "",
                r.Status, r.CreatedByUserId, r.AssignedToUserId?.ToString() ?? "",
                r.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.CreatedAt.ToString("o")));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
