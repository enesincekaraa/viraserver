using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;

namespace Vira.Application.Features.Requests;
public sealed record AdminUpdateCommand(Guid id, int? Status, Guid? AssignedToUserId) : IRequest<bool>;

public sealed class AdminUpdateHandler : IRequestHandler<AdminUpdateCommand, bool>
{
    private readonly IRepository<Request> _repo;
    private readonly IUnitOfWork _uow;

    public AdminUpdateHandler(IRepository<Request> repo, IUnitOfWork uow)
        => (_repo, _uow) = (repo, uow);

    public async Task<bool> Handle(AdminUpdateCommand c, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(c.id, ct);
        if (r is null || r.IsDeleted) return false;

        if (c.AssignedToUserId.HasValue)
            r.AssignToUser(c.AssignedToUserId.Value);

        if (c.Status.HasValue)
        {
            switch ((RequestStatus)c.Status.Value)
            {
                case RequestStatus.Resolved: r.Resolve(); break;
                case RequestStatus.Rejected: r.Reject(); break;
                case RequestStatus.Assigned:
                    r.AssignToUser(r.AssignedToUserId); // Optionally re-assign to current user
                    r.UpdatedAt = DateTime.UtcNow;
                    break;
                case RequestStatus.Open:
                    r.Open();
                    r.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        _repo.UpdateAsync(r);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
