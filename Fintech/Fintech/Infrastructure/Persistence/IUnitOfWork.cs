using System;
using System.Threading.Tasks;

namespace Fintech.Infrastructure.Persistence;

public interface IUnitOfWork : IDisposable
{
    IBaseRepository<TEntity> Repository<TEntity>() where TEntity : class;
    Task<int> CompleteAsync();
}
