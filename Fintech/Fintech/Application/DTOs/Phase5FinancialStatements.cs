using System;
using System.Collections.Generic;

namespace Fintech.Application.DTOs;

/// <summary>
/// Trial Balance DTO - shows all GL accounts with debit/credit totals
/// </summary>
public class Phase5TrialBalanceResult
{
    public bool IsBalanced { get; set; }
    public long TotalDebits { get; set; }
    public long TotalCredits { get; set; }
    public List<Phase5TrialBalanceAccount> Accounts { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Single account in Trial Balance
/// </summary>
public class Phase5TrialBalanceAccount
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long Debits { get; set; }
    public long Credits { get; set; }
    public long Balance { get; set; }  // Debits - Credits
}

/// <summary>
/// Balance Sheet Result showing Assets, Liabilities, and Equity
/// </summary>
public class Phase5BalanceSheetResult
{
    public DateTime AsOfDate { get; set; }
    public Phase5BalanceSheetSection Assets { get; set; } = new();
    public Phase5BalanceSheetSection Liabilities { get; set; } = new();
    public Phase5BalanceSheetSection Equity { get; set; } = new();
    
    public long TotalAssets { get; set; }
    public long TotalLiabilities { get; set; }
    public long TotalEquity { get; set; }
    
    public bool IsBalanced { get; set; }  // Assets = Liabilities + Equity
    public string ValidationMessage { get; set; } = string.Empty;
}

/// <summary>
/// Section of Balance Sheet (Assets, Liabilities, or Equity)
/// </summary>
public class Phase5BalanceSheetSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<Phase5BalanceSheetAccount> Accounts { get; set; } = new();
    public long Subtotal { get; set; }
}

/// <summary>
/// Account in Balance Sheet Section
/// </summary>
public class Phase5BalanceSheetAccount
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;  // Asset, Liability, Equity, etc.
    public long Balance { get; set; }  // In Paise
    public long OpeningBalance { get; set; }  // Optional: opening balance for comparison
}

/// <summary>
/// Account Statement Result showing account activity over a period
/// </summary>
public class Phase5AccountStatementResult
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    public long OpeningBalance { get; set; }
    public long ClosingBalance { get; set; }
    public long TotalDebits { get; set; }
    public long TotalCredits { get; set; }
    
    public List<Phase5AccountStatementLine> Transactions { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Single transaction line in Account Statement
/// </summary>
public class Phase5AccountStatementLine
{
    public DateTime Date { get; set; }
    public string EntryCode { get; set; } = string.Empty;  // Journal Entry Code
    public string Description { get; set; } = string.Empty;
    public long Debit { get; set; }  // In Paise
    public long Credit { get; set; }  // In Paise
    public long RunningBalance { get; set; }  // In Paise
}

/// <summary>
/// Request to generate financial statement reports
/// </summary>
public class GenerateFinancialReportRequest
{
    public Guid BranchId { get; set; }
    public DateTime AsOfDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Export PDF Request
/// </summary>
public class ExportPdfRequest
{
    public Guid BranchId { get; set; }
    public string ReportType { get; set; } = string.Empty;  // trial-balance, balance-sheet, account-statement
    public DateTime AsOfDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? AccountCode { get; set; }  // For account statement export
}

/// <summary>
/// Email Report Request
/// </summary>
public class EmailReportRequest
{
    public Guid BranchId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public List<string> EmailRecipients { get; set; } = new();
    public DateTime AsOfDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? AccountCode { get; set; }
}
