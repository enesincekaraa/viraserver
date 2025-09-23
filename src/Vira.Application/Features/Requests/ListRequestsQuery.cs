using MediatR;
using System.Linq.Expressions;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Requests;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;

public sealed record ListRequestsQuery(
    int Page = 1, int PageSize = 20,
    int? Status = null,
    Guid? CategoryId = null,
    Guid? CreatedBy = null,
    Guid? AssignedTo = null) : IRequest<Result<PagedResult<RequestResponse>>>;

public sealed class ListRequestsHandler : IRequestHandler<ListRequestsQuery, Result<PagedResult<RequestResponse>>>
{
    private readonly IReadRepository<Request> _read;
    public ListRequestsHandler(IReadRepository<Request> read) => _read = read;

    public async Task<Result<PagedResult<RequestResponse>>> Handle(ListRequestsQuery q, CancellationToken ct)
    {
        Expression<Func<Request, bool>> pred = r => true;
        if (q.Status is not null) pred = And(pred, r => r.Status == (RequestStatus)q.Status);
        if (q.CategoryId is not null) pred = And(pred, r => r.CategoryId == q.CategoryId);
        if (q.CreatedBy is not null) pred = And(pred, r => r.CreatedByUserId == q.CreatedBy);
        if (q.AssignedTo is not null) pred = And(pred, r => r.AssignedToUserId == q.AssignedTo);

        var (items, total) = await _read.ListPagedAsync(q.Page, q.PageSize,
            predicate: pred, orderBy: s => s.OrderByDescending(x => x.CreatedAt), ct);

        var dtos = items.Select(e => new RequestResponse(
            e.Id, e.Title, e.Description, e.CategoryId, (int)e.Status, e.CreatedByUserId, e.AssignedToUserId, e.Latitude, e.Longitude)).ToList();

        return Result<PagedResult<RequestResponse>>.Success(new(dtos, q.Page, q.PageSize, total));
    }

    private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
    {
        var p = Expression.Parameter(typeof(T));
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(Expression.Invoke(a, p), Expression.Invoke(b, p)), p);
    }
}
