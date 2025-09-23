using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Categories;
using Vira.Shared;

namespace Vira.Application.Features.Category;
public sealed record CreateCategoryCommand(string Name, string? Description) : IRequest<Result<CategoryResponse>>;




public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);

    }
}

public sealed class CreateCategoryCommandHandler(IRepository<Vira.Domain.Entities.Category> _repo, IUnitOfWork _uow)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new Vira.Domain.Entities.Category(request.Name, request.Description);
        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<CategoryResponse>.Success(new CategoryResponse(entity.Id, entity.Name, entity.Description));
    }
}