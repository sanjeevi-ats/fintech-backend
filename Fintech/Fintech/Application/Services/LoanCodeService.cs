using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Infrastructure.Persistence;
using Fintech.Infrastructure.Logging;

namespace Fintech.Application.Services;

public interface ILoanCodeService
{
    /// <summary>
    /// Generates the next loan code in sequence (LN00001, LN00002, etc.)
    /// </summary>
    Task<string> GenerateNextLoanCodeAsync();

    /// <summary>
    /// Retrieves a loan by its loan code
    /// </summary>
    Task<Guid?> GetLoanIdByCodeAsync(string loanCode);
}

public class LoanCodeService : ILoanCodeService
{
    private readonly FinVedaDbContext _context;
    private readonly IFileLoggerService _fileLogger;

    public LoanCodeService(FinVedaDbContext context, IFileLoggerService fileLogger)
    {
        _context = context;
        _fileLogger = fileLogger;
    }

    public async Task<string> GenerateNextLoanCodeAsync()
    {
        try
        {
            var stopwatch = LoggingHelper.StartTimer();

            // Get next sequence value
            var result = await _context.Database.SqlQueryRaw<long>(
                "SELECT nextval('loan_code_seq')"
            ).ToListAsync();

            if (result.Count == 0)
            {
                throw new InvalidOperationException("Failed to get sequence value");
            }

            var sequenceValue = result[0];
            var loanCode = $"LN{sequenceValue:D5}";

            var executionTime = LoggingHelper.StopTimer(stopwatch);
            await _fileLogger.LogInfoAsync(
                "Loan Code Generation",
                nameof(LoanCodeService),
                nameof(GenerateNextLoanCodeAsync),
                responseData: new { loanCode, sequenceValue },
                executionTimeMs: executionTime,
                successMessage: "Loan code generated successfully"
            );

            return loanCode;
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
            await _fileLogger.LogErrorAsync(
                "Loan Code Generation",
                nameof(LoanCodeService),
                nameof(GenerateNextLoanCodeAsync),
                ex,
                executionTimeMs: executionTime
            );

            throw;
        }
    }

    public async Task<Guid?> GetLoanIdByCodeAsync(string loanCode)
    {
        try
        {
            var stopwatch = LoggingHelper.StartTimer();

            var loan = await _context.LoanCases
                .Where(l => l.LoanCode == loanCode)
                .Select(l => l.Id)
                .FirstOrDefaultAsync();

            var executionTime = LoggingHelper.StopTimer(stopwatch);

            if (loan == Guid.Empty)
            {
                await _fileLogger.LogWarningAsync(
                    "Loan Code Lookup",
                    nameof(LoanCodeService),
                    nameof(GetLoanIdByCodeAsync),
                    $"Loan not found with code: {loanCode}",
                    executionTime
                );
                return null;
            }

            await _fileLogger.LogInfoAsync(
                "Loan Code Lookup",
                nameof(LoanCodeService),
                nameof(GetLoanIdByCodeAsync),
                requestParameters: new { loanCode },
                responseData: new { loanId = loan },
                executionTimeMs: executionTime,
                successMessage: "Loan code lookup successful"
            );

            return loan;
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
            await _fileLogger.LogErrorAsync(
                "Loan Code Lookup",
                nameof(LoanCodeService),
                nameof(GetLoanIdByCodeAsync),
                ex,
                requestParameters: new { loanCode },
                executionTimeMs: executionTime
            );

            throw;
        }
    }
}
