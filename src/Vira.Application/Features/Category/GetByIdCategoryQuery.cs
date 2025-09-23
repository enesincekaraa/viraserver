using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Categories;
using Vira.Shared;

namespace Vira.Application.Features.Category;
public sealed record GetByIdCategoryQuery(Guid Id) : IRequest<Result<CategoryResponse>>;


public sealed class GetCategoryByIdValidator : AbstractValidator<GetByIdCategoryQuery>
{
    public GetCategoryByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Category Id is required.");
    }
}


public sealed class GetByIdCategoryHandler(IReadRepository<Vira.Domain.Entities.Category> _repo) : IRequestHandler<GetByIdCategoryQuery, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(GetByIdCategoryQuery request, CancellationToken cancellationToken)
    {
        var e = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (e is null)
        {
            return Result<CategoryResponse>.Failure("NotFound", "Category not found");
        }
        return Result<CategoryResponse>.Success(new CategoryResponse(e.Id, e.Name, e.Description));
    }
}