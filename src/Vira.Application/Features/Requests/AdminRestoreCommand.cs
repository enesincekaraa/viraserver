using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;

namespace Vira.Application.Features.Requests;
public sealed record AdminRestoreCommand(Guid Id) : IRequest<bool>;

public sealed class AdminRestoreHandler : IRequestHandler<AdminRestoreCommand, bool>
{
    private readonly IRepository<Request> _repo;
    private readonly IUnitOfWork _uow;
    public AdminRestoreHandler(IRepository<Request> repo, IUnitOfWork uow)
        => (_repo, _uow) = (repo, uow);

    public async Task<bool> Handle(AdminRestoreCommand c, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(c.Id, ct);
        if (r is null || !r.IsDeleted) return false;
        r.IsDeleted = false; r.UpdatedAt = DateTime.UtcNow;
        _repo.UpdateAsync(r);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}