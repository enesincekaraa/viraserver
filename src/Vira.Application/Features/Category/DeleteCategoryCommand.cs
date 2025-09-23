using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Shared;

namespace Vira.Application.Features.Category;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IRepository<Vira.Domain.Entities.Category> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteCategoryHandler(IRepository<Vira.Domain.Entities.Category> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(DeleteCategoryCommand req, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(req.Id, ct);
        if (entity is null) return Result.Failure("Category.NotFound", "Kategori bulunamadı.");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
