using MediatR;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;
public sealed record DeleteRequestCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteRequestHandler(IRepository<Request> _repo, IUnitOfWork _uow, ICurrentUser _me)
    : IRequestHandler<DeleteRequestCommand, Result>
{
    public async Task<Result> Handle(DeleteRequestCommand request, CancellationToken cancellationToken)
    {

        var e = await _repo.GetByIdAsync(request.Id, cancellationToken);

        if (e is null)
            return Result.Success();

        if (!_me.IsAdmin && (!_me.UserId.HasValue || e.CreatedByUserId != _me.UserId.Value))
            return Result.Failure("forbidden", "You are not allowed to delete this request.");

        e.IsDeleted = true;
        e.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();


    }
}

