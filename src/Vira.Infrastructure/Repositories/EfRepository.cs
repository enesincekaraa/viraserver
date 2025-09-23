using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Vira.Application.Abstractions.Repositories;
using Vira.Shared.Base;

namespace Vira.Infrastructure.Repositories;

public class EfRepository<T> : IRepository<T> where T : BaseEntity<Guid>
{
    private readonly AppDbContext _db;
    private readonly DbSet<T> _set;

    public EfRepository(AppDbContext db) { _db = db; _set = db.Set<T>(); }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _set.FirstOrDefaultAsync(x => x.Id.Equals(id), ct);

    public async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null
         ? await _set.AsNoTracking().ToListAsync(ct)
         : await _set.AsNoTracking().Where(predicate).ToListAsync(ct);


    public async Task<(IReadOnlyList<T> Items, int TotalCount)> ListPagedAsync(
    int page, int pageSize,
    Expression<Func<T, bool>>? predicate = null,
    Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
    CancellationToken ct = default)
    {
        IQueryable<T> q = _set.AsNoTracking();

        if (predicate is not null)
            q = q.Where(predicate);

        var total = await q.CountAsync(ct);

        if (orderBy is not null)
            q = orderBy(q);
        else
            q = q.OrderBy(e => e.Id); // default stabil sıralama

        var items = await q.Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    { _set.Update(entity); return Task.CompletedTask; }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    { _set.Remove(entity); return Task.CompletedTask; }
}
