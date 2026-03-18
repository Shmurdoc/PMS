using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Infrastructure.Repositories;

// ═══════════════════════════════════════════════════════════════════════
//  SPECIFICATION EVALUATOR — Translates specs into EF Core queries
// ═══════════════════════════════════════════════════════════════════════
public static class SpecificationEvaluator<T> where T : Entity
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        if (specification.Skip.HasValue)
            query = query.Skip(specification.Skip.Value);

        if (specification.Take.HasValue)
            query = query.Take(specification.Take.Value);

        return query;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GENERIC REPOSITORY — EF Core implementation
// ═══════════════════════════════════════════════════════════════════════
public class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { id }, ct);

    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default) =>
        await DbSet.AsNoTracking().ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default) =>
        await SpecificationEvaluator<T>.GetQuery(DbSet.AsQueryable(), spec).ToListAsync(ct);

    public virtual async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default) =>
        await SpecificationEvaluator<T>.GetQuery(DbSet.AsQueryable(), spec).FirstOrDefaultAsync(ct);

    public virtual async Task<int> CountAsync(ISpecification<T>? spec = null, CancellationToken ct = default) =>
        spec is null
            ? await DbSet.CountAsync(ct)
            : await SpecificationEvaluator<T>.GetQuery(DbSet.AsQueryable(), spec).CountAsync(ct);

    public virtual async Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default) =>
        await SpecificationEvaluator<T>.GetQuery(DbSet.AsQueryable(), spec).AnyAsync(ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) =>
        await DbSet.AddRangeAsync(entities, ct);

    public virtual void Update(T entity) => DbSet.Update(entity);
    public virtual void Remove(T entity) => DbSet.Remove(entity);
    public virtual void RemoveRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);
}
