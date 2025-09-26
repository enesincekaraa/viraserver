using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using Vira.Application.Abstractions.Persistence;

namespace Vira.Application.Features.Assists;

public sealed class AdminExportCsv
{

    public sealed record AdminExportCsvQuery(int? Status, int? Type, string? Search)
        : IRequest<(byte[] Content, string FileName, string ContentType)>;

    public sealed class AdminExportCsvHandler
        : IRequestHandler<AdminExportCsvQuery, (byte[] Content, string FileName, string ContentType)>
    {
        private readonly IReadDb _db;
        public AdminExportCsvHandler(IReadDb db) => _db = db;

        public async Task<(byte[] Content, string FileName, string ContentType)> Handle(
            AdminExportCsvQuery q, CancellationToken ct)
        {
            var s = _db.AssistTickets.AsNoTracking();

            if (q.Status.HasValue) s = s.Where(x => (int)x.Status == q.Status);
            if (q.Type.HasValue) s = s.Where(x => (int)x.Type == q.Type);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var t = q.Search.Trim();
                s = s.Where(x => x.ElderFullName.Contains(t) || x.Address.Contains(t));
            }

            var list = await s.OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    Type = (int)x.Type,
                    Status = (int)x.Status,
                    x.CreatedByUserId,
                    x.ElderFullName,
                    x.ElderPhone,
                    x.Address,
                    x.Latitude,
                    x.Longitude,
                    x.AssignedToUserId,
                    x.ScheduledAtUtc,
                    CreatedAtUtc = x.CreatedAt,
                    x.Notes
                })
                .ToListAsync(ct);

            string Csv(string? v)
            {
                if (v is null) return "";
                var needsQuote = v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r');
                v = v.Replace("\"", "\"\"");
                return needsQuote ? $"\"{v}\"" : v;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Id,Type,Status,CreatedByUserId,ElderFullName,ElderPhone,Address,Latitude,Longitude,AssignedToUserId,ScheduledAtUtc,CreatedAtUtc,Notes");

            foreach (var r in list)
            {
                sb.Append(r.Id).Append(',')
                  .Append(r.Type).Append(',')
                  .Append(r.Status).Append(',')
                  .Append(r.CreatedByUserId).Append(',')
                  .Append(Csv(r.ElderFullName)).Append(',')
                  .Append(Csv(r.ElderPhone)).Append(',')
                  .Append(Csv(r.Address)).Append(',')
                  .Append(r.Latitude.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(r.Longitude.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(r.AssignedToUserId?.ToString() ?? "").Append(',')
                  .Append(r.ScheduledAtUtc?.ToString("O") ?? "").Append(',')
                  .Append(r.CreatedAtUtc.ToString("O")).Append(',')
                  .Append(Csv(r.Notes))
                  .AppendLine();
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var file = $"assist_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return (bytes, file, "text/csv; charset=utf-8");
        }
    }

}
