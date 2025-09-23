using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;
public sealed record UpdateRequestCommand(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId
 ) : IRequest<Result>;

public sealed class UpdateRequestValidator : AbstractValidator<UpdateRequestCommand>
{
    public UpdateRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(200);
        RuleFor(x => x.CategoryId).NotEmpty();

    }
}

public sealed class UpdateRequestCommandHandler(IRepository<Request> _repo, IUnitOfWork _uow, ICurrentUser _me) : IRequestHandler<UpdateRequestCommand, Result>
{
    public async Task<Result> Handle(UpdateRequestCommand request, CancellationToken cancellationToken)
    {
        var e = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (e is null)
            return Result.Failure("Request not found.", "Talep bulunamadı");

        if (!_me.IsAdmin && (!_me.UserId.HasValue || e.CreatedByUserId != _me.UserId.Value))
            return Result.Failure("You are not authorized to update this request.", "Bu talebi güncelleme yetkiniz yok");

        if (e.Status == RequestStatus.Resolved)
            return Result.Failure("Only open requests can be updated.", "Sadece açık talepler güncellenebilir");

        e.Update(request.Title, request.Description, request.CategoryId);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
