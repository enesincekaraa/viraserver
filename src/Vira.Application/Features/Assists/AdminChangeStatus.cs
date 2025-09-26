using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;

namespace Vira.Application.Features.Assists;
public sealed class AdminChangeStatus
{

    public sealed record AdminChangeStatusCommand(Guid Id, int Status, string? Reason) : IRequest<bool>;

    public sealed class AdminChangeStatusHandler : IRequestHandler<AdminChangeStatusCommand, bool>
    {
        private readonly IRepository<AssistTicket> _repo;
        private readonly IUnitOfWork _uow;

        public AdminChangeStatusHandler(IRepository<AssistTicket> repo, IUnitOfWork uow)
        { _repo = repo; _uow = uow; }

        public async Task<bool> Handle(AdminChangeStatusCommand c, CancellationToken ct)
        {
            if (!Enum.IsDefined(typeof(AssistStatus), c.Status))
                throw new ValidationException("Status geçersiz.");

            var e = await _repo.GetByIdAsync(c.Id, ct);
            if (e is null || e.IsDeleted) return false;

            e.ChangeStatus((AssistStatus)c.Status, c.Reason);
            await _repo.UpdateAsync(e, ct);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }

}
