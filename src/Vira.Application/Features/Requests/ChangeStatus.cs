using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;

public sealed record AssignRequestCommand(Guid Id, Guid? AssignedToUserId) : IRequest<Result>;
public sealed record ResolveRequestCommand(Guid Id) : IRequest<Result>;
public sealed record RejectRequestCommand(Guid Id) : IRequest<Result>;

public sealed class AssignRequestHandler : IRequestHandler<AssignRequestCommand, Result>
{
    private readonly IRepository<Request> _repo; private readonly IUnitOfWork _uow;
    public AssignRequestHandler(IRepository<Request> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(AssignRequestCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct); if (e is null) return Result.Failure("Request.NotFound", "Talep bulunamadı.");
        e.AssignToUser(c.AssignedToUserId); await _repo.UpdateAsync(e, ct); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}
public sealed class ResolveRequestHandler : IRequestHandler<ResolveRequestCommand, Result>
{
    private readonly IRepository<Request> _repo; private readonly IUnitOfWork _uow;
    public ResolveRequestHandler(IRepository<Request> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(ResolveRequestCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct); if (e is null) return Result.Failure("Request.NotFound", "Talep bulunamadı.");
        e.Resolve(); await _repo.UpdateAsync(e, ct); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}
public sealed class RejectRequestHandler : IRequestHandler<RejectRequestCommand, Result>
{
    private readonly IRepository<Request> _repo; private readonly IUnitOfWork _uow;
    public RejectRequestHandler(IRepository<Request> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(RejectRequestCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct); if (e is null) return Result.Failure("Request.NotFound", "Talep bulunamadı.");
        e.Reject(); await _repo.UpdateAsync(e, ct); await _uow.SaveChangesAsync(ct); return Result.Success();
    }
}
