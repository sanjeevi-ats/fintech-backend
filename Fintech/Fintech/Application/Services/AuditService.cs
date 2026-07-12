using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IAuditService
{
    Task<AuditLog?> GetByCodeAsync(string code);
    Task<IReadOnlyList<AuditLog>> GetEntityHistoryAsync(string entityName, string recordId);
    Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 100);
}

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;

    public AuditService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ICodeGenerationService codeService,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _codeService = codeService;
        _tenantService = tenantService;
    }

    public async Task<AuditLog?> GetByCodeAsync(string code)
    {
        return await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.AuditLogCode == code && a.BranchId == _tenantService.BranchId);
    }

    public async Task<IReadOnlyList<AuditLog>> GetEntityHistoryAsync(string entityName, string recordId)
    {
        return await _dbContext.AuditLogs
            .Where(x => x.TableName == entityName && x.RecordId == recordId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _dbContext.AuditLogs
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
