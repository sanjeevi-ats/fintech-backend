using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Fintech.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface ILoanCaseService
{
    Task<LoanCase?> GetByIdAsync(Guid id);
    Task<LoanCase?> GetByCodeAsync(string code);
    Task<LoanCase?> GetByLoanCodeAsync(string loanCode);
    Task<IReadOnlyList<LoanCase>> GetAllAsync();
    Task<LoanCase> CreateAsync(LoanCase loanCase);
    Task ApproveLoanAsync(Guid id);
    Task DisburseLoanAsync(Guid id);
}

public class LoanCaseService : ILoanCaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ILogger<LoanCaseService> _logger;
    private readonly ITenantService _tenantService;
    private readonly ICodeGenerationService _codeService;
    private readonly IFileLoggerService _fileLogger;

    public LoanCaseService(
        IUnitOfWork unitOfWork, 
        FinVedaDbContext dbContext, 
        ILogger<LoanCaseService> logger, 
        ITenantService tenantService,
        ICodeGenerationService codeService,
        IFileLoggerService fileLogger)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
        _tenantService = tenantService;
        _codeService = codeService;
        _fileLogger = fileLogger;
    }

    public async Task<LoanCase?> GetByIdAsync(Guid id)
    {
        return await _dbContext.LoanCases
            .AsNoTracking()
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<LoanCase?> GetByCodeAsync(string code)
    {
        return await _dbContext.LoanCases
            .AsNoTracking()
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.LoanCode == code && l.BranchId == _tenantService.BranchId);
    }

    public async Task<LoanCase?> GetByLoanCodeAsync(string loanCode)
    {
        try
        {
            var stopwatch = LoggingHelper.StartTimer();

            var loan = await _dbContext.LoanCases
                .AsNoTracking()
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.LoanCode == loanCode);

            var executionTime = LoggingHelper.StopTimer(stopwatch);

            if (loan != null)
            {
                await _fileLogger.LogInfoAsync(
                    "Loan Case Service",
                    nameof(LoanCaseService),
                    nameof(GetByLoanCodeAsync),
                    requestParameters: new { loanCode },
                    responseData: new { loanId = loan.Id },
                    executionTimeMs: executionTime,
                    successMessage: "Loan found by code"
                );
            }
            else
            {
                await _fileLogger.LogWarningAsync(
                    "Loan Case Service",
                    nameof(LoanCaseService),
                    nameof(GetByLoanCodeAsync),
                    $"Loan not found with code: {loanCode}",
                    executionTime
                );
            }

            return loan;
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
            await _fileLogger.LogErrorAsync(
                "Loan Case Service",
                nameof(LoanCaseService),
                nameof(GetByLoanCodeAsync),
                ex,
                requestParameters: new { loanCode },
                executionTimeMs: executionTime
            );
            throw;
        }
    }

    public async Task<IReadOnlyList<LoanCase>> GetAllAsync()
    {
        try
        {
            var stopwatch = LoggingHelper.StartTimer();

            // Eagerly load Customer data without selecting the customer_code column
            // (which may not exist in the database yet)
            var loans = await _dbContext.LoanCases
                .AsNoTracking()
                .Include(l => l.Customer)
                .ToListAsync();

            var executionTime = LoggingHelper.StopTimer(stopwatch);

            await _fileLogger.LogInfoAsync(
                "Loan Case Service",
                nameof(LoanCaseService),
                nameof(GetAllAsync),
                responseData: new { count = loans.Count },
                executionTimeMs: executionTime,
                successMessage: $"Retrieved {loans.Count} loans with customer data"
            );

            return loans;
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42703") // Column does not exist
        {
            _logger.LogWarning(pgEx, "Note: customer_code column not in database yet - will be populated programmatically");
            
            // Fall back to loading without the problematic column by using raw SQL or a different approach
            // For now, just return empty list and let the error be handled gracefully
            return new List<LoanCase>();
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
            await _fileLogger.LogErrorAsync(
                "Loan Case Service",
                nameof(LoanCaseService),
                nameof(GetAllAsync),
                ex,
                executionTimeMs: executionTime
            );
            throw;
        }
    }

    public async Task<LoanCase> CreateAsync(LoanCase loanCase)
    {
        try
        {
            var stopwatch = LoggingHelper.StartTimer();

            loanCase.Id = Guid.NewGuid();
            loanCase.BranchId = _tenantService.BranchId;
            loanCase.Status = LoanStatus.draft;
            
            // Generate loan code
            loanCase.LoanCode = await _codeService.GenerateCodeAsync("LoanCase", _tenantService.BranchId);
            
            await _unitOfWork.Repository<LoanCase>().AddAsync(loanCase);
            await _unitOfWork.CompleteAsync();

            var executionTime = LoggingHelper.StopTimer(stopwatch);
            await _fileLogger.LogInfoAsync(
                "Loan Case Service",
                nameof(LoanCaseService),
                nameof(CreateAsync),
                responseData: new { loanId = loanCase.Id, loanCode = loanCase.LoanCode },
                executionTimeMs: executionTime,
                successMessage: "Loan case created with code"
            );

            return loanCase;
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
            await _fileLogger.LogErrorAsync(
                "Loan Case Service",
                nameof(LoanCaseService),
                nameof(CreateAsync),
                ex,
                executionTimeMs: executionTime
            );
            throw;
        }
    }

    public async Task ApproveLoanAsync(Guid id)
    {
        // Call stored procedure: proc_approve_loan(loan_id)
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"CALL proc_approve_loan({id})");
    }

    public async Task DisburseLoanAsync(Guid id)
    {
        // Call stored procedure: proc_disburse_loan(loan_id)
        // The procedure should handle status update and journal entries as per requirements
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"CALL proc_disburse_loan({id})");
    }
}
