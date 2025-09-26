using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;

namespace Vira.Application.Features.Assists;
public sealed class AdminAssing
{

    public sealed record AdminAssignCommand(Guid Id, Guid AssignedToUserId) : IRequest<bool>;

    public sealed class AdminAssignHandler : IRequestHandler<AdminAssignCommand, bool>
    {
        private readonly IRepository<AssistTicket> _repo;
        private readonly IUnitOfWork _uow;

        public AdminAssignHandler(IRepository<AssistTicket> repo, IUnitOfWork uow)
        { _repo = repo; _uow = uow; }

        public async Task<bool> Handle(AdminAssignCommand c, CancellationToken ct)
        {
            var e = await _repo.GetByIdAsync(c.Id, ct);
            if (e is null || e.IsDeleted) return false;

            e.Assign(c.AssignedToUserId);
            await _repo.UpdateAsync(e, ct);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }

}
