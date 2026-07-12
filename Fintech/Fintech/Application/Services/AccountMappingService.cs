using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

public interface IAccountMappingService
{
    Task<string> GetGLAccountForCapitalAsync(string capitalAccountCode);
    
    Task<bool> ValidateAccountMappingAsync(string capitalAccountCode);
    
    Task MapAllCapitalAccountsAsync(string glAccountCode);
    
    Task<AccountMapping> CreateAccountMappingAsync(
        string capitalAccountCode,
        Guid capitalAccountId,
        string glAccountCode,
        Guid glAccountId,
        Guid branchId);
}

public class AccountMappingService : IAccountMappingService
{
    private readonly FinVedaDbContext _dbContext;
    private readonly ITenantService _tenantService;

    public const string DEFAULT_CAPITAL_GL_ACCOUNT = "GL-3000";  // Capital/Equity account

    public AccountMappingService(
        FinVedaDbContext dbContext,
        ITenantService tenantService)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Gets the GL account code for a capital account.
    /// Returns the mapped GL account code or empty string if not found.
    /// </summary>
    public async Task<string> GetGLAccountForCapitalAsync(string capitalAccountCode)
    {
        if (string.IsNullOrEmpty(capitalAccountCode))
            throw new FinVedaException(400, "BAD_REQUEST", "Capital account code cannot be empty");

        var mapping = await _dbContext.AccountMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => 
                m.CapitalAccountCode == capitalAccountCode && 
                m.IsActive);

        return mapping?.GLAccountCode ?? string.Empty;
    }

    /// <summary>
    /// Validates that a capital account has an active mapping.
    /// </summary>
    public async Task<bool> ValidateAccountMappingAsync(string capitalAccountCode)
    {
        if (string.IsNullOrEmpty(capitalAccountCode))
            return false;

        var glAccountCode = await GetGLAccountForCapitalAsync(capitalAccountCode);
        return !string.IsNullOrEmpty(glAccountCode);
    }

    /// <summary>
    /// Maps all capital accounts to a specific GL account.
    /// Useful for bulk initialization.
    /// </summary>
    public async Task MapAllCapitalAccountsAsync(string glAccountCode)
    {
        if (string.IsNullOrEmpty(glAccountCode))
            throw new FinVedaException(400, "BAD_REQUEST", "GL account code cannot be empty");

        // Verify GL account exists
        var glAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountCode == glAccountCode);

        if (glAccount == null)
            throw new FinVedaException(404, "GL_ACCOUNT_NOT_FOUND", $"GL account {glAccountCode} not found");

        // Get all unmapped capital accounts
        var unmappedAccounts = await _dbContext.CapitalAccounts
            .Where(ca => !_dbContext.AccountMappings
                .Any(am => am.CapitalAccountId == ca.Id && am.IsActive))
            .ToListAsync();

        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                foreach (var capitalAccount in unmappedAccounts)
                {
                    var mapping = new AccountMapping
                    {
                        Id = Guid.NewGuid(),
                        BranchId = _tenantService.BranchId,
                        CapitalAccountCode = capitalAccount.CapitalAccountCode,
                        CapitalAccountId = capitalAccount.Id,
                        GLAccountCode = glAccountCode,
                        GLAccountId = glAccount.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.AccountMappings.Add(mapping);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a single account mapping.
    /// </summary>
    public async Task<AccountMapping> CreateAccountMappingAsync(
        string capitalAccountCode,
        Guid capitalAccountId,
        string glAccountCode,
        Guid glAccountId,
        Guid branchId)
    {
        if (string.IsNullOrEmpty(capitalAccountCode))
            throw new FinVedaException(400, "BAD_REQUEST", "Capital account code cannot be empty");

        if (string.IsNullOrEmpty(glAccountCode))
            throw new FinVedaException(400, "BAD_REQUEST", "GL account code cannot be empty");

        // Check if mapping already exists
        var existingMapping = await _dbContext.AccountMappings
            .FirstOrDefaultAsync(m => 
                m.CapitalAccountCode == capitalAccountCode && 
                m.BranchId == branchId);

        if (existingMapping != null)
            throw new FinVedaException(400, "BAD_REQUEST", 
                "Mapping already exists for this capital account");

        // Verify GL account exists
        var glAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountCode == glAccountCode);

        if (glAccount == null)
            throw new FinVedaException(404, "GL_ACCOUNT_NOT_FOUND", $"GL account {glAccountCode} not found");

        var mapping = new AccountMapping
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            CapitalAccountCode = capitalAccountCode,
            CapitalAccountId = capitalAccountId,
            GLAccountCode = glAccountCode,
            GLAccountId = glAccountId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AccountMappings.Add(mapping);
        await _dbContext.SaveChangesAsync();

        return mapping;
    }
}
