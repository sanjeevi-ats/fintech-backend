using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace Fintech.Application.Features.Reports;

public class GetPortfolioAtRiskHandler : IRequestHandler<GetPortfolioAtRiskQuery, object>
{
    private readonly IConfiguration _config;
    public GetPortfolioAtRiskHandler(IConfiguration config) => _config = config;

    public async Task<object> Handle(GetPortfolioAtRiskQuery request, CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        string sql = @"
            WITH overdue_days AS (
                SELECT 
                    lc.id,
                    lc.principal,
                    lc.amount_paid,
                    EXTRACT(DAY FROM (CURRENT_DATE - MIN(i.due_date))) AS days_overdue
                FROM loan_cases lc
                JOIN installments i ON lc.id = i.loan_case_id
                WHERE lc.branch_id = @BranchId 
                  AND lc.status = 'active'
                  AND i.status = 'pending'
                  AND i.due_date < CURRENT_DATE
                GROUP BY lc.id, lc.principal, lc.amount_paid
            )
            SELECT 
                SUM(CASE WHEN days_overdue BETWEEN 1 AND 30 THEN (principal - amount_paid) ELSE 0 END) AS bucket_30,
                SUM(CASE WHEN days_overdue BETWEEN 31 AND 60 THEN (principal - amount_paid) ELSE 0 END) AS bucket_60,
                SUM(CASE WHEN days_overdue BETWEEN 61 AND 90 THEN (principal - amount_paid) ELSE 0 END) AS bucket_90,
                SUM(CASE WHEN days_overdue > 90 THEN (principal - amount_paid) ELSE 0 END) AS npa_90_plus
            FROM overdue_days;";

        var data = await connection.QueryFirstOrDefaultAsync(sql, new { request.BranchId });
        return data ?? new { bucket_30 = 0, bucket_60 = 0, bucket_90 = 0, npa_90_plus = 0 };
    }
}

public class GetCollectionEfficiencyHandler : IRequestHandler<GetCollectionEfficiencyQuery, object>
{
    private readonly IConfiguration _config;
    public GetCollectionEfficiencyHandler(IConfiguration config) => _config = config;

    public async Task<object> Handle(GetCollectionEfficiencyQuery request, CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        string sql = @"
            WITH expected AS (
                SELECT SUM(amount) AS expected_amount
                FROM installments
                WHERE branch_id = @BranchId AND due_date BETWEEN @StartDate AND @EndDate
            ),
            actual AS (
                SELECT SUM(amount_paid) AS actual_amount
                FROM receipts
                WHERE branch_id = @BranchId AND captured_at BETWEEN @StartDate AND @EndDate
            )
            SELECT 
                COALESCE(e.expected_amount, 0) AS TargetAmount, 
                COALESCE(a.actual_amount, 0) AS CollectedAmount,
                CASE WHEN COALESCE(e.expected_amount, 0) = 0 THEN 0 
                     ELSE ROUND((COALESCE(a.actual_amount, 0) * 100.0) / e.expected_amount, 2) 
                END AS EfficiencyPercentage
            FROM expected e CROSS JOIN actual a;";

        var data = await connection.QueryFirstOrDefaultAsync(sql, new 
        { 
            request.BranchId, 
            StartDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30), 
            EndDate = request.EndDate ?? DateTime.UtcNow 
        });
        return data ?? new { TargetAmount = 0, CollectedAmount = 0, EfficiencyPercentage = 0 };
    }
}

public class GetTrialBalanceReportHandler : IRequestHandler<GetTrialBalanceReportQuery, object>
{
    private readonly IConfiguration _config;
    public GetTrialBalanceReportHandler(IConfiguration config) => _config = config;

    public async Task<object> Handle(GetTrialBalanceReportQuery request, CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        string sql = @"
            SELECT 
                a.code AS AccountCode, 
                a.name AS AccountName,
                SUM(CASE WHEN jl.type = 'debit' THEN jl.amount ELSE 0 END) AS TotalDebit,
                SUM(CASE WHEN jl.type = 'credit' THEN jl.amount ELSE 0 END) AS TotalCredit,
                SUM(CASE WHEN jl.type = 'debit' THEN jl.amount ELSE -jl.amount END) AS NetBalance
            FROM journal_lines jl
            JOIN accounts a ON jl.account_id = a.id
            WHERE jl.branch_id = @BranchId
            GROUP BY a.code, a.name
            ORDER BY a.code;";

        var data = await connection.QueryAsync(sql, new { request.BranchId });
        return data;
    }
}

public class GetDisbursementTrendsHandler : IRequestHandler<GetDisbursementTrendsQuery, object>
{
    private readonly IConfiguration _config;
    public GetDisbursementTrendsHandler(IConfiguration config) => _config = config;

    public async Task<object> Handle(GetDisbursementTrendsQuery request, CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        // Assuming disbursed_at isn't explicit but created_at on JournalEntry or similar could work.
        // We'll safely use created_at from loan_cases for trends demo.
        string sql = @"
            SELECT 
                TO_CHAR(created_at, 'YYYY-MM') AS Month,
                COUNT(id) AS LoanCount,
                SUM(principal) AS TotalDisbursed
            FROM loan_cases
            WHERE branch_id = @BranchId AND status = 'active'
            GROUP BY TO_CHAR(created_at, 'YYYY-MM')
            ORDER BY Month DESC
            LIMIT @PageSize OFFSET @Offset;";

        var offset = (request.PageNumber - 1) * request.PageSize;
        var data = await connection.QueryAsync(sql, new { request.BranchId, request.PageSize, Offset = offset });
        return data;
    }
}

public class GetAgentProductivityHandler : IRequestHandler<GetAgentProductivityQuery, object>
{
    private readonly IConfiguration _config;
    public GetAgentProductivityHandler(IConfiguration config) => _config = config;

    public async Task<object> Handle(GetAgentProductivityQuery request, CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        // Assuming branch_id links to users who made actions
        string sql = @"
            SELECT 
                'Agent_Productivity_Metric' AS Metric,
                COUNT(id) AS TotalLoansSourced,
                SUM(principal) AS SourcedVolume
            FROM loan_cases
            WHERE branch_id = @BranchId;";

        var data = await connection.QueryAsync(sql, new { request.BranchId });
        return data;
    }
}
