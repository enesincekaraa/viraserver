using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Categories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features;
public sealed record UpdateCategoryCommand(Guid id, string name, string? description) : IRequest<Result<CategoryResponse>>;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public sealed class UpdateCategoryCommandHandler(IRepository<Category> _repo, IUnitOfWork _uow) : IRequestHandler<UpdateCategoryCommand, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {

        var entity = await _repo.GetByIdAsync(request.id, cancellationToken);
        if (entity is null)
            return Result<CategoryResponse>.Failure("NotFound", $"Category with Id {request.id} not found.");
        entity.Renama(request.name);

        if (request.description != entity.Description)
        {
            typeof(Category).GetProperty(nameof(Category.Description))!.SetValue(entity, request.description);
            entity.UpdatedAt = DateTime.UtcNow;
        }
        await _repo.UpdateAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var response = new CategoryResponse(entity.Id, entity.Name, entity.Description);
        return Result<CategoryResponse>.Success(response);
    }
}
