using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;

namespace Vira.Application.Features.Requests;
public sealed record AdminSoftDeleteCommand(Guid Id) : IRequest<bool>;

public sealed class AdminSoftDeleteHandler : IRequestHandler<AdminSoftDeleteCommand, bool>
{
    private readonly IRepository<Request> _repo;
    private readonly IUnitOfWork _uow;
    public AdminSoftDeleteHandler(IRepository<Request> repo, IUnitOfWork uow)
        => (_repo, _uow) = (repo, uow);

    public async Task<bool> Handle(AdminSoftDeleteCommand c, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(c.Id, ct);
        if (r is null || r.IsDeleted) return false;
        r.SoftDelete(Guid.Empty); // veya CurrentUser.UserId
        _repo.UpdateAsync(r);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}


