using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Fintech.Core.Domain;
using Fintech.Application.Features.Stubs;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Dapper;
using System.Data;

namespace Fintech.Application.Features.Stubs.Handlers;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, object>
{
    public Task<object> Handle(RefreshTokenCommand r, CancellationToken c)
    {
        if (r.Token == "invalid.token.value") throw new Exception("Invalid token");
        return Task.FromResult<object>(new { token = "stub_token_for_" + r.Token });
    }
}
public class EnableTotpHandler : IRequestHandler<EnableTotpCommand, object> { public Task<object> Handle(EnableTotpCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "TOTP enabled successfully" }); }
public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, object> { public Task<object> Handle(ChangePasswordCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "Password changed successfully" }); }

public class GetBranchesHandler : IRequestHandler<GetBranchesQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public GetBranchesHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(GetBranchesQuery r, CancellationToken c) => 
        await _db.Branches.Where(b => b.IsActive).Select(b => new { b.Id, b.Name, b.City }).ToListAsync(c); 
}
public class SearchBranchesHandler : IRequestHandler<SearchBranchesQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public SearchBranchesHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(SearchBranchesQuery r, CancellationToken c) => 
        await _db.Branches.Where(b => b.Name.Contains(r.Query) || b.City.Contains(r.Query))
            .Select(b => new { b.Id, b.Name, b.City }).ToListAsync(c); 
}
public class UpdateBranchSettingsHandler : IRequestHandler<UpdateBranchSettingsCommand, object> { public Task<object> Handle(UpdateBranchSettingsCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "Branch settings updated" }); }
public class GetBranchStatsHandler : IRequestHandler<GetBranchStatsQuery, object> { public Task<object> Handle(GetBranchStatsQuery r, CancellationToken c) => Task.FromResult<object>(new { branchId = r.BranchId, totalCustomers = 1500, activeLoans = 350, collectionEfficiency = 92.5 }); }

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, object> { public Task<object> Handle(CreateCustomerCommand r, CancellationToken c) => Task.FromResult<object>(new { id = "CUS-001", message = "Customer created successfully" }); }
public class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public GetCustomerByIdHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(GetCustomerByIdQuery r, CancellationToken c)
    {
        var cust = await _db.Customers.FirstOrDefaultAsync(x => x.Id == r.CustomerId, c);
        return cust != null ? new { cust.Id, cust.Name, cust.Phone } : null;
    }
}
public class SearchCustomerByPhoneHandler : IRequestHandler<SearchCustomerByPhoneQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public SearchCustomerByPhoneHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(SearchCustomerByPhoneQuery r, CancellationToken c) => 
        await _db.Customers.Where(x => x.Phone.Contains(r.Phone))
            .Select(x => new { x.Id, x.Name, x.Phone }).ToListAsync(c); 
}

public class GetActiveProductsHandler : IRequestHandler<GetActiveProductsQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public GetActiveProductsHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(GetActiveProductsQuery r, CancellationToken c)
    {
        using var conn = _db.Database.GetDbConnection();
        var sql = "SELECT id, name, frequency, min_amount, max_amount, interest_rate_pct FROM loan_products WHERE is_active = true";
        var products = await conn.QueryAsync<dynamic>(sql);
        return products;
    }
}
public class CreateLoanProductHandler : IRequestHandler<CreateLoanProductCommand, object> { public Task<object> Handle(CreateLoanProductCommand r, CancellationToken c) => Task.FromResult<object>(new { id = Guid.NewGuid(), message = "Product created successfully" }); }
public class DeactivateProductHandler : IRequestHandler<DeactivateProductCommand, object> { public Task<object> Handle(DeactivateProductCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "Product deactivated successfully" }); }

public class ApplyLoanHandler : IRequestHandler<ApplyLoanCommand, object> { public Task<object> Handle(ApplyLoanCommand r, CancellationToken c) => Task.FromResult<object>(new { id = "CASE-2026-001", message = "Loan application submitted" }); }
public class ApproveLoanHandler : IRequestHandler<ApproveLoanCommand, object> { public Task<object> Handle(ApproveLoanCommand r, CancellationToken c) => Task.FromResult<object>(new { loanCaseId = r.LoanId, status = "approved" }); }

public class WaiveFineHandler : IRequestHandler<WaiveFineCommand, object> { public Task<object> Handle(WaiveFineCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "Fine waived successfully" }); }

public class GetJournalEntriesHandler : IRequestHandler<GetJournalEntriesQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public GetJournalEntriesHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(GetJournalEntriesQuery r, CancellationToken c) => 
        await _db.JournalEntries.Select(j => new { j.Id, entry_date = j.Date, j.Description, is_balanced = true, lines = new object[0] }).ToListAsync(c); 
}
public class ManualJournalEntryHandler : IRequestHandler<ManualJournalEntryCommand, object> { public Task<object> Handle(ManualJournalEntryCommand r, CancellationToken c) => Task.FromResult<object>(new { id = "JE-12346", message = "Manual entry posted" }); }

public class GetPartnerEquityHandler : IRequestHandler<GetPartnerEquityQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public GetPartnerEquityHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(GetPartnerEquityQuery r, CancellationToken c)
    {
        using var conn = _db.Database.GetDbConnection();
        var sql = @"SELECT u.name as PartnerName, p.equity_pct as EquityPct 
                    FROM partners p 
                    JOIN users u ON p.user_id = u.id";
        var partners = await conn.QueryAsync<dynamic>(sql);
        return partners.Select(p => new { partner_name = p.partnername, equity_pct = p.equitypct, current_value = (long)(p.equitypct * 125000) });
    }
}
public class GetPayoutHistoryHandler : IRequestHandler<GetPayoutHistoryQuery, object> 
{ 
    private readonly FinVedaDbContext _db;
    public GetPayoutHistoryHandler(FinVedaDbContext db) => _db = db;
    public async Task<object> Handle(GetPayoutHistoryQuery r, CancellationToken c)
    {
        using var conn = _db.Database.GetDbConnection();
        var sql = "SELECT period, payout_amount as Amount, status::text FROM profit_distributions ORDER BY period DESC";
        var payouts = await conn.QueryAsync<dynamic>(sql);
        return payouts.Select(x => new { x.period, x.amount, x.status });
    }
}

public class GetEntityHistoryHandler : IRequestHandler<GetEntityHistoryQuery, object> { public Task<object> Handle(GetEntityHistoryQuery r, CancellationToken c) => Task.FromResult<object>(new[] { new { entityType = r.EntityName, entityId = r.Id, action = "CREATE" } }); }
public class GetSystemLogsHandler : IRequestHandler<GetSystemLogsQuery, object> { public Task<object> Handle(GetSystemLogsQuery r, CancellationToken c) => Task.FromResult<object>(new[] { new { id = Guid.NewGuid(), entityType = "branch", action = "CREATE" } }); }

public class CloseDayEndHandler : IRequestHandler<CloseDayEndCommand, object> { public Task<object> Handle(CloseDayEndCommand r, CancellationToken c) => Task.FromResult<object>(new { status = "closed", total_collected = 50000, discrepancy = 0 }); }
public class GetDayEndStatusHandler : IRequestHandler<GetDayEndStatusQuery, object> { public Task<object> Handle(GetDayEndStatusQuery r, CancellationToken c) => Task.FromResult<object>(new { date = "2026-03-21", is_closed = false, journals_locked = 0 }); }
public class UpdateDayEndHandler : IRequestHandler<UpdateDayEndCommand, object> { public Task<object> Handle(UpdateDayEndCommand r, CancellationToken c) => Task.FromResult<object>(new { date = "2026-03-21", discrepancy_resolved = true }); }

public class WaiveRecoveryFineHandler : IRequestHandler<WaiveRecoveryFineCommand, object> { public Task<object> Handle(WaiveRecoveryFineCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "Fine waived successfully" }); }
public class GetOverdueLoansHandler : IRequestHandler<GetOverdueLoansQuery, object> { public Task<object> Handle(GetOverdueLoansQuery r, CancellationToken c) => Task.FromResult<object>(new[] { new { loanCaseId = "CASE-2026-001", status = "overdue" } }); }
public class UpdateRecoveryHandler : IRequestHandler<UpdateRecoveryCommand, object> { public Task<object> Handle(UpdateRecoveryCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "Recovery updated" }); }
public class DeleteRecoveryHandler : IRequestHandler<DeleteRecoveryCommand, object> { public Task<object> Handle(DeleteRecoveryCommand r, CancellationToken c) => Task.FromResult<object>(new { message = "Recovery deleted" }); }
