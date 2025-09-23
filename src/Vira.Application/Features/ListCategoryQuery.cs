using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Categories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Categories;

public sealed record ListCategoriesQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<Result<PagedResult<CategoryResponse>>>;

public sealed class ListCategoriesHandler
    : IRequestHandler<ListCategoriesQuery, Result<PagedResult<CategoryResponse>>>
{
    private readonly IReadRepository<Category> _read;
    public ListCategoriesHandler(IReadRepository<Category> read) => _read = read;

    public async Task<Result<PagedResult<CategoryResponse>>> Handle(ListCategoriesQuery q, CancellationToken ct)
    {
        // basit filtre: name contains
        var (items, total) = await _read.ListPagedAsync(
            q.Page, q.PageSize,
            predicate: string.IsNullOrWhiteSpace(q.Search)
                ? null
                : c => c.Name.ToLower().Contains(q.Search!.ToLower()),
            orderBy: src => src.OrderBy(c => c.Name),
            ct);

        var dtoItems = items.Select(e => new CategoryResponse(e.Id, e.Name, e.Description)).ToList();
        return Result<PagedResult<CategoryResponse>>.Success(
            new PagedResult<CategoryResponse>(dtoItems, q.Page, q.PageSize, total));
    }
}
