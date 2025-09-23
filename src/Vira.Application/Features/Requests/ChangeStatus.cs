using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;

public sealed record AssignRequestCommand(Guid Id, Guid? AssignedToUserId, Guid PerformedByUserId) : IRequest<Result>;
public sealed record ResolveRequestCommand(Guid Id, Guid PerformedByUserId) : IRequest<Result>;
public sealed record RejectRequestCommand(Guid Id, Guid PerformedByUserId) : IRequest<Result>;


public sealed class AssignRequestHandler : IRequestHandler<AssignRequestCommand, Result>
{
    private readonly IRepository<Request> _repo;
    private readonly IRepository<RequestComment> _comments;
    private readonly IUnitOfWork _uow;

    public AssignRequestHandler(IRepository<Request> repo, IRepository<RequestComment> comments, IUnitOfWork uow)
    { _repo = repo; _comments = comments; _uow = uow; }
    public async Task<Result> Handle(AssignRequestCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return Result.Failure("Request.NotFound", "Talep bulunamadı.");

        e.AssignToUser(c.AssignedToUserId);
        await _repo.UpdateAsync(e, ct);

        await _comments.AddAsync(
            new RequestComment(e.Id, c.PerformedByUserId, "Talep atandı.", CommentType.SystemNote), ct);

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
public sealed class ResolveRequestHandler : IRequestHandler<ResolveRequestCommand, Result>
{
    private readonly IRepository<Request> _repo;
    private readonly IRepository<RequestComment> _comments;
    private readonly IUnitOfWork _uow;

    public ResolveRequestHandler(IRepository<Request> repo, IRepository<RequestComment> comments, IUnitOfWork uow)
    { _repo = repo; _comments = comments; _uow = uow; }

    public async Task<Result> Handle(ResolveRequestCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return Result.Failure("Request.NotFound", "Talep bulunamadı.");

        e.Resolve();
        await _repo.UpdateAsync(e, ct);

        await _comments.AddAsync(
            new RequestComment(e.Id, c.PerformedByUserId, "Talep çözüldü.", CommentType.SystemNote), ct);

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class RejectRequestHandler : IRequestHandler<RejectRequestCommand, Result>
{
    private readonly IRepository<Request> _repo;
    private readonly IRepository<RequestComment> _comments;
    private readonly IUnitOfWork _uow;

    public RejectRequestHandler(IRepository<Request> repo, IRepository<RequestComment> comments, IUnitOfWork uow)
    { _repo = repo; _comments = comments; _uow = uow; }

    public async Task<Result> Handle(RejectRequestCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return Result.Failure("Request.NotFound", "Talep bulunamadı.");

        e.Reject();
        await _repo.UpdateAsync(e, ct);

        await _comments.AddAsync(
            new RequestComment(e.Id, c.PerformedByUserId, "Talep reddedildi.", CommentType.SystemNote), ct);

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

