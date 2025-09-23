using System.Linq.Expressions;
using Vira.Shared.Base;

namespace Vira.Application.Abstractions.Repositories;

public interface IReadRepository<T> where T : BaseEntity<Guid>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<(IReadOnlyList<T> Items, int TotalCount)> ListPagedAsync(
        int page, int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken ct = default);
}

public interface IRepository<T> : IReadRepository<T> where T : BaseEntity<Guid>
{
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
