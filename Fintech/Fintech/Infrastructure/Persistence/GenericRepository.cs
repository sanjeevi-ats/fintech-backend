using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Infrastructure.Persistence;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly FinVedaDbContext _dbContext;

    public GenericRepository(FinVedaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Set<T>().FindAsync(id);
    }

    public async Task<T?> GetByIdIgnoreFiltersAsync(Guid id)
    {
        // For entities with an Id property, ignore query filters
        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, "Id");
        var constant = Expression.Constant(id);
        var equals = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
        
        return await _dbContext.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(lambda);
    }

    public async Task<IReadOnlyList<T>> ListAllAsync()
    {
        return await _dbContext.Set<T>().ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbContext.Set<T>().AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }
}
