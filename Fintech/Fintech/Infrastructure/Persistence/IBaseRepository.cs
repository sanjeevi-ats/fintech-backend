using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fintech.Infrastructure.Persistence;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<T?> GetByIdIgnoreFiltersAsync(Guid id);
    Task<IReadOnlyList<T>> ListAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
