using MediatR;
using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Persistence;
using Vira.Contracts.Assists;

namespace Vira.Application.Features.Assists;
public sealed class GetById
{

    public sealed record AdminGetByIdQuery(Guid Id) : IRequest<AssistsDtos.AssistResponse>;

    public sealed class AdminGetByIdHandler
        : IRequestHandler<AdminGetByIdQuery, AssistsDtos.AssistResponse>
    {
        private readonly IReadDb _db;
        public AdminGetByIdHandler(IReadDb db) => _db = db;

        public async Task<AssistsDtos.AssistResponse> Handle(AdminGetByIdQuery q, CancellationToken ct)
        {
            var e = await _db.AssistTickets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

            if (e is null) throw new KeyNotFoundException();

            return new AssistsDtos.AssistResponse(
                e.Id, (int)e.Type, (int)e.Status, e.CreatedByUserId,
                e.ElderFullName, e.ElderPhone, e.Address,
                e.Latitude, e.Longitude, e.AssignedToUserId,
                e.ScheduledAtUtc, e.Notes, e.CreatedAt);
        }
    }

}
