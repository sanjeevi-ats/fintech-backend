using System;
using System.Collections;
using System.Threading.Tasks;

namespace Fintech.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly FinVedaDbContext _context;
    private Hashtable _repositories;

    public UnitOfWork(FinVedaDbContext context)
    {
        _context = context;
        _repositories = new Hashtable();
    }

    public async Task<int> CompleteAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public IBaseRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity).Name;

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(BaseRepository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _context);
            _repositories.Add(type, repositoryInstance);
        }

        return (IBaseRepository<TEntity>)_repositories[type]!;
    }
}
