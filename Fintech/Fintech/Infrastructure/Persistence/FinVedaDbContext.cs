using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Application.Services;

namespace Fintech.Infrastructure.Persistence;

public class FinVedaDbContext : DbContext
{
    private readonly ITenantService _tenantService;

    public FinVedaDbContext(DbContextOptions<FinVedaDbContext> options, ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<LoanCase> LoanCases { get; set; }
    public DbSet<Installment> Installments { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<JournalLine> JournalLines { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Partner> Partners { get; set; }
    public DbSet<CapitalAccount> CapitalAccounts { get; set; }
    public DbSet<CapitalTransaction> CapitalTransactions { get; set; }
    public DbSet<CapitalAccountHistory> CapitalAccountHistories { get; set; }
    public DbSet<LoanProduct> LoanProducts { get; set; }
    public DbSet<DayEnd> DayEnds { get; set; }
    public DbSet<ProfitDistribution> ProfitDistributions { get; set; }
    public DbSet<CodeSequence> CodeSequences { get; set; }
    public DbSet<CollectionRequest> CollectionRequests { get; set; }
    // Phase 5: Double-Entry Accounting
    public DbSet<AccountMapping> AccountMappings { get; set; }
    public DbSet<LedgerBalance> LedgerBalances { get; set; }
    public DbSet<LedgerHistory> LedgerHistories { get; set; }
    
    // Phase 6: Month-End Close System
    public DbSet<AccountingPeriod> AccountingPeriods { get; set; }
    public DbSet<AccrualEntry> AccrualEntries { get; set; }
    public DbSet<ProvisionEntry> ProvisionEntries { get; set; }
    public DbSet<PeriodReversal> PeriodReversals { get; set; }

    // Phase 7: Interest Accounting System
    public DbSet<InterestCalculation> InterestCalculations { get; set; }
    public DbSet<InterestPosting> InterestPostings { get; set; }
    public DbSet<InterestWaiver> InterestWaivers { get; set; }

    // Phase 8: P&L Statements
    public DbSet<ProfitLossStatement> ProfitLossStatements { get; set; }
    public DbSet<RevenueLine> RevenueLines { get; set; }
    public DbSet<ExpenseLine> ExpenseLines { get; set; }

    // Phase 9: Cash Flow Analysis
    public DbSet<CashFlowStatement> CashFlowStatements { get; set; }
    public DbSet<CashFlowItem> CashFlowItems { get; set; }
    public DbSet<CashFlowForecast> CashFlowForecasts { get; set; }

    public DbSet<CompanySettings> CompanySettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {


        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.UserCode).IsUnique();
        });

        // CustomerCode is mapped to database column customer_code

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.PartnerCode).IsUnique();
        });

        modelBuilder.Entity<LoanCase>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.LoanCode).IsUnique();
        });

        modelBuilder.Entity<Installment>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.InstallmentCode).IsUnique();
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.Property(e => e.Mode).HasConversion<string>();
            entity.HasIndex(e => e.ReceiptCode).IsUnique();
        });

        modelBuilder.Entity<CapitalAccount>(entity =>
        {
            entity.HasIndex(e => e.CapitalAccountCode).IsUnique();
            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.CapitalAccount)
                .HasForeignKey(t => t.CapitalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.History)
                .WithOne(h => h.CapitalAccount)
                .HasForeignKey(h => h.CapitalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CapitalTransaction>(entity =>
        {
            entity.HasIndex(e => e.TransactionCode).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CapitalAccountHistory>(entity =>
        {
            // Composite key to ensure one history entry per account per date
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CapitalAccountId, e.EffectiveDate }).IsUnique();
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(e => e.AccountCode).IsUnique();
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasIndex(e => e.JournalEntryCode).IsUnique();
        });

        modelBuilder.Entity<JournalLine>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasIndex(e => e.JournalLineCode).IsUnique();
        });

        modelBuilder.Entity<LoanProduct>(entity =>
        {
            entity.Property(e => e.RepaymentFrequency).HasConversion<string>();
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(e => e.BranchCode).IsUnique();
        });

        modelBuilder.Entity<DayEnd>(entity =>
        {
            entity.HasIndex(e => e.DayEndCode).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.AuditLogCode).IsUnique();
        });

        modelBuilder.Entity<ProfitDistribution>(entity =>
        {
            entity.HasIndex(e => e.ProfitDistributionCode).IsUnique();
        });

        modelBuilder.Entity<CollectionRequest>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PaymentMode).HasConversion<string>();
            entity.HasIndex(e => e.RequestNumber).IsUnique();
            
            entity.HasOne(e => e.Installment)
                .WithMany()
                .HasForeignKey(e => e.InstallmentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.LoanCase)
                .WithMany()
                .HasForeignKey(e => e.LoanCaseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.RequestedBy)
                .WithMany()
                .HasForeignKey(e => e.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ApprovedBy)
                .WithMany()
                .HasForeignKey(e => e.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 5: Configure AccountMapping
        modelBuilder.Entity<AccountMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BranchId, e.CapitalAccountCode }).IsUnique();
            entity.HasIndex(e => e.GLAccountCode);
        });

        // Phase 5: Configure LedgerBalance with optimistic locking
        modelBuilder.Entity<LedgerBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BranchId, e.GLAccountCode }).IsUnique();
            entity.Property(e => e.Version).IsConcurrencyToken();
        });

        // Phase 5: Configure LedgerHistory
        modelBuilder.Entity<LedgerHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BranchId, e.GLAccountCode, e.Date });
        });

        // Phase 6: Configure AccountingPeriod
        modelBuilder.Entity<AccountingPeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodCode).IsUnique();
            entity.HasIndex(e => new { e.BranchId, e.Status });
            entity.HasMany(e => e.AccrualEntries)
                .WithOne(a => a.Period)
                .HasForeignKey(a => a.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.ProvisionEntries)
                .WithOne(p => p.Period)
                .HasForeignKey(p => p.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Reversals)
                .WithOne(r => r.Period)
                .HasForeignKey(r => r.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 6: Configure AccrualEntry
        modelBuilder.Entity<Fintech.Core.Domain.AccrualEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.LoanId);
            entity.HasIndex(e => e.Type);
            entity.HasOne(e => e.Period)
                .WithMany(p => p.AccrualEntries)
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Loan)
                .WithMany()
                .HasForeignKey(e => e.LoanId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.JournalEntry)
                .WithMany()
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 6: Configure ProvisionEntry
        modelBuilder.Entity<Fintech.Core.Domain.ProvisionEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.LoanId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Period)
                .WithMany(p => p.ProvisionEntries)
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Loan)
                .WithMany()
                .HasForeignKey(e => e.LoanId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.JournalEntry)
                .WithMany()
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 6: Configure PeriodReversal
        modelBuilder.Entity<PeriodReversal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Period)
                .WithMany(p => p.Reversals)
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 7: Configure InterestCalculation
        modelBuilder.Entity<InterestCalculation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LoanId);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.CalculationDate);
            entity.Property(e => e.DailyRate).HasPrecision(10, 6);
            entity.HasOne(e => e.Loan)
                .WithMany()
                .HasForeignKey(e => e.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Phase 7: Configure InterestPosting
        modelBuilder.Entity<InterestPosting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LoanId);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.PostedDate);
            entity.HasOne(e => e.Loan)
                .WithMany()
                .HasForeignKey(e => e.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.JournalEntry)
                .WithMany()
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 7: Configure InterestWaiver
        modelBuilder.Entity<InterestWaiver>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LoanId);
            entity.HasIndex(e => e.WaiverDate);
            entity.HasOne(e => e.Loan)
                .WithMany()
                .HasForeignKey(e => e.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReversalJournalEntry)
                .WithMany()
                .HasForeignKey(e => e.ReversalJournalEntryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure CodeSequence with unique constraint on (EntityName, BranchId)
        modelBuilder.Entity<CodeSequence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityName, e.BranchId }).IsUnique();
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CodePrefix).IsRequired().HasMaxLength(10);
        });

        // Company Settings
        modelBuilder.Entity<CompanySettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ReceiptPrefix).HasMaxLength(20).HasDefaultValue("RCP");
            entity.Property(e => e.ReceiptLanguage).HasMaxLength(10).HasDefaultValue("en");
        });

        base.OnModelCreating(modelBuilder);

        // Apply Global Query Filters for BranchId
        modelBuilder.Entity<User>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        
        modelBuilder.Entity<LoanCase>()
            .HasQueryFilter(e => e.BranchId == _tenantService.BranchId);

        modelBuilder.Entity<Installment>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<Branch>().HasQueryFilter(e => e.Id == _tenantService.BranchId);
        modelBuilder.Entity<JournalEntry>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<Account>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<JournalLine>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<Receipt>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<Partner>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        // CapitalAccount does not have BranchId in Phase 4
        modelBuilder.Entity<LoanProduct>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<DayEnd>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<ProfitDistribution>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<CollectionRequest>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);

        // Phase 5: Apply query filters for new entities
        modelBuilder.Entity<AccountMapping>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<LedgerBalance>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<LedgerHistory>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);

        // Phase 6: Apply query filters
        modelBuilder.Entity<Fintech.Core.Domain.AccountingPeriod>().HasQueryFilter(e => e.BranchId == _tenantService.BranchId);
        modelBuilder.Entity<Fintech.Core.Domain.AccrualEntry>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.ProvisionEntry>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.PeriodReversal>().HasQueryFilter(e => true);

        // Phase 7: Apply query filters
        modelBuilder.Entity<Fintech.Core.Domain.InterestCalculation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.InterestPosting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.InterestWaiver>().HasQueryFilter(e => !e.IsDeleted);

        // Phase 8: Configure ProfitLossStatement
        modelBuilder.Entity<Fintech.Core.Domain.ProfitLossStatement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.StatementDate);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.RevenueLines)
                .WithOne(r => r.ProfitLossStatement)
                .HasForeignKey(r => r.ProfitLossStatementId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ExpenseLines)
                .WithOne(e => e.ProfitLossStatement)
                .HasForeignKey(e => e.ProfitLossStatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Phase 8: Configure RevenueLine
        modelBuilder.Entity<Fintech.Core.Domain.RevenueLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProfitLossStatementId);
            entity.HasIndex(e => e.Category);
        });

        // Phase 8: Configure ExpenseLine
        modelBuilder.Entity<Fintech.Core.Domain.ExpenseLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProfitLossStatementId);
            entity.HasIndex(e => e.Category);
        });

        // Phase 8: Apply query filters
        modelBuilder.Entity<Fintech.Core.Domain.ProfitLossStatement>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.RevenueLine>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.ExpenseLine>().HasQueryFilter(e => !e.IsDeleted);

        // Phase 9: Configure CashFlowStatement
        modelBuilder.Entity<Fintech.Core.Domain.CashFlowStatement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.StatementDate);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.CashFlowItems)
                .WithOne(c => c.CashFlowStatement)
                .HasForeignKey(c => c.CashFlowStatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Phase 9: Configure CashFlowItem
        modelBuilder.Entity<Fintech.Core.Domain.CashFlowItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CashFlowStatementId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.ItemType);
        });

        // Phase 9: Configure CashFlowForecast
        modelBuilder.Entity<Fintech.Core.Domain.CashFlowForecast>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.ForecastPeriod);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Phase 9: Apply query filters
        modelBuilder.Entity<Fintech.Core.Domain.CashFlowStatement>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.CashFlowItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Fintech.Core.Domain.CashFlowForecast>().HasQueryFilter(e => !e.IsDeleted);

        // Configure AES-256 Encryption Value Converters
        modelBuilder.Entity<Customer>()
            .Property(c => c.Aadhaar_Encrypted)
            .HasConversion(
                v => EncryptionHelper.Encrypt(v),
                v => EncryptionHelper.Decrypt(v)
            );

        modelBuilder.Entity<Customer>()
            .Property(c => c.PAN_Encrypted)
            .HasConversion(
                v => EncryptionHelper.Encrypt(v),
                v => EncryptionHelper.Decrypt(v)
            );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        var auditLogs = new List<AuditLog>();
        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog) continue;

            var before = entry.State == EntityState.Added ? "{}" : JsonSerializer.Serialize(entry.OriginalValues.ToObject());
            var after = entry.State == EntityState.Deleted ? "{}" : JsonSerializer.Serialize(entry.CurrentValues.ToObject());

            // For Branch entities, use the Branch's own ID as BranchId
            Guid branchId = _tenantService.BranchId;
            if (entry.Entity is Branch branch)
            {
                branchId = branch.Id;
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                RecordId = entry.Property("Id").CurrentValue?.ToString() ?? "",
                Action = entry.State.ToString(),
                BeforeVal = before,
                AfterVal = after,
                Timestamp = DateTime.UtcNow
            };
            
            auditLogs.Add(auditLog);
        }

        // Save the main changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Insert audit logs using raw SQL to bypass query filters completely
        // This ensures audit logs are created even when the BranchId doesn't match the current tenant
        if (auditLogs.Any())
        {
            foreach (var log in auditLogs)
            {
                // Use FormattableString interpolation for proper parameter binding
                await Database.ExecuteSqlAsync(
                    $@"INSERT INTO audit_logs (id, branch_id, table_name, record_id, action, before_val, after_val, timestamp) 
                       VALUES ({log.Id}, {log.BranchId}, {log.TableName}, {log.RecordId}, {log.Action}, {log.BeforeVal}, {log.AfterVal}, {log.Timestamp})");
            }
        }

        return result;
    }
}
