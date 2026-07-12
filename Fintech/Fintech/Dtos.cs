using Fintech.Core.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fintech;

// --- SHARED ---
public class BaseResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}

// --- USERS ---
public class UserDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class RegisterRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    [Required] public Guid BranchId { get; set; }
    public string Role { get; set; } = "agent";
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

// --- CUSTOMERS ---
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Aadhaar { get; set; } = string.Empty; // Masked
    public string Pan { get; set; } = string.Empty; // Masked
}

public class CustomerRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    // Either plain or encrypted version can be provided from frontend
    public string? Aadhaar { get; set; }
    public string? AadhaarEncrypted { get; set; }
    public string? Pan { get; set; }
    public string? PanEncrypted { get; set; }
    public Guid BranchId { get; set; }
}

// --- LOAN CASES ---
public class LoanCaseDto
{
    public Guid Id { get; set; }
    public string? LoanCode { get; set; } // LN00001, LN00002, etc.
    public string? CustomerCode { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public long Principal { get; set; }
    public long InterestAmount { get; set; }
    public long TotalReceivable { get; set; }
    public long ProcessingFees { get; set; }
    public string Status { get; set; } = string.Empty;

    // Workflow metadata
    public Guid? SubmittedById { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? DocumentUrls { get; set; }
}

public class CreateLoanRequest
{
    [Required] public Guid CustomerId { get; set; }
    [Required] public long Principal { get; set; }
    [Required] public long InterestAmount { get; set; }
    [Required] public long ProcessingFees { get; set; }
    public string? DocumentUrls { get; set; }
}

// --- INSTALLMENTS ---
public class InstallmentDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int No { get; set; }
    public DateTime DueDate { get; set; }
    public long Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

// --- RECEIPTS ---
public class RecordPaymentRequest
{
    [Required] public Guid InstallmentId { get; set; }
    [Required] public long AmountPaid { get; set; }
    [Required] public string Mode { get; set; } = "cash"; // cash, upi, bank_transfer
    public string UtrRef { get; set; } = string.Empty;
}

public class ReceiptDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public long AmountPaid { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string UtrRef { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}

// --- PARTNERS ---
public class PartnerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public double EquityPct { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PartnerRequest
{
    [Required] public Guid UserId { get; set; }
    [Required] public double EquityPct { get; set; }
}

// --- CAPITAL ACCOUNTS ---
public class InvestmentRequest
{
    [Required] public Guid PartnerId { get; set; }
    [Required] public long Amount { get; set; }
    public string PaymentMode { get; set; } = "cash";
    public string Remarks { get; set; } = string.Empty;
}

public class WithdrawalRequest
{
    [Required] public Guid PartnerId { get; set; }
    [Required] public long Amount { get; set; }
    public string PaymentMode { get; set; } = "cash";
    public string Remarks { get; set; } = string.Empty;
}

public class PartnerCapitalSummaryDto
{
    public Guid PartnerId { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public long TotalInvestment { get; set; }
    public long TotalProfit { get; set; }
    public long TotalWithdrawal { get; set; }
    public long CurrentBalance { get; set; }
}

// --- PHASE 4: CAPITAL ACCOUNT MANAGEMENT ---

/// <summary>
/// Request to create a new capital account
/// </summary>
public class CreateCapitalAccountRequest
{
    [Required] public Guid PartnerId { get; set; }
    [Required] public long OpeningBalance { get; set; }  // in Paise
}

/// <summary>
/// Request to record a capital transaction
/// </summary>
public class RecordCapitalTransactionRequest
{
    [Required] public string Type { get; set; } = string.Empty;  // Contribution, Withdrawal, Distribution, Adjustment
    [Required] public long Amount { get; set; }  // in Paise
    [Required] public DateTime TransactionDate { get; set; }
    [Required] public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
}

/// <summary>
/// Capital Account DTO for API responses
/// </summary>
public class CapitalAccountDto
{
    public Guid Id { get; set; }
    public string CapitalAccountCode { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public long OpeningBalance { get; set; }
    public long CurrentBalance { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Capital Transaction DTO for API responses
/// </summary>
public class CapitalTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public Guid CapitalAccountId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public long Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Capital Account Overview DTO for dashboard
/// </summary>
public class CapitalAccountOverviewDto
{
    public long TotalCapitalContributed { get; set; }
    public int ActiveInvestors { get; set; }
    public long AverageInvestorCapital { get; set; }
    public List<CapitalAccountDto> Investors { get; set; } = new();
}

/// <summary>
/// Ownership Percentage DTO
/// </summary>
public class OwnershipPercentageDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public long Capital { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public long TotalCapital { get; set; }
}

// --- PHASE 8: P&L STATEMENTS ---

/// <summary>
/// P&L Statement DTO
/// </summary>
public class ProfitLossStatementDto
{
    public Guid Id { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? BranchId { get; set; }
    public long TotalRevenue { get; set; }
    public long TotalExpenses { get; set; }
    public long GrossProfit { get; set; }
    public long NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public DateTime StatementDate { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public List<RevenueLineDto> RevenueLines { get; set; } = new();
    public List<ExpenseLineDto> ExpenseLines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Revenue Line DTO
/// </summary>
public class RevenueLineDto
{
    public Guid Id { get; set; }
    public int Category { get; set; }
    public string? CategoryName { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
}

/// <summary>
/// Expense Line DTO
/// </summary>
public class ExpenseLineDto
{
    public Guid Id { get; set; }
    public int Category { get; set; }
    public string? CategoryName { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
}

/// <summary>
/// Request to generate P&L statement
/// </summary>
public class GeneratePLStatementRequest
{
    [Required] public Guid PeriodId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Revenue Summary DTO
/// </summary>
public class RevenueSummaryDto
{
    public long InterestRevenue { get; set; }
    public long FeeRevenue { get; set; }
    public long PenaltyRevenue { get; set; }
    public long OtherRevenue { get; set; }
    public long TotalRevenue { get; set; }
}

/// <summary>
/// Expense Summary DTO
/// </summary>
public class ExpenseSummaryDto
{
    public long ProvisionExpense { get; set; }
    public long WaiverExpense { get; set; }
    public long OperatingExpense { get; set; }
    public long AdminExpense { get; set; }
    public long TotalExpense { get; set; }
}

/// <summary>
/// P&L Comparison DTO (for multiple periods)
/// </summary>
public class PLComparisonDto
{
    public Guid PeriodId1 { get; set; }
    public Guid PeriodId2 { get; set; }
    public ProfitLossStatementDto Statement1 { get; set; } = new();
    public ProfitLossStatementDto Statement2 { get; set; } = new();
    public long RevenueChange { get; set; }
    public long ExpenseChange { get; set; }
    public long ProfitChange { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public decimal ProfitChangePercent { get; set; }
}

// --- PHASE 9: CASH FLOW ANALYSIS ---

/// <summary>
/// Cash Flow Statement DTO
/// </summary>
public class CashFlowStatementDto
{
    public Guid Id { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? BranchId { get; set; }
    public long OperatingCashFlow { get; set; }
    public long InvestingCashFlow { get; set; }
    public long FinancingCashFlow { get; set; }
    public long NetCashFlow { get; set; }
    public long BeginningBalance { get; set; }
    public long EndingBalance { get; set; }
    public DateTime StatementDate { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public List<CashFlowItemDto> CashFlowItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Cash Flow Item DTO
/// </summary>
public class CashFlowItemDto
{
    public Guid Id { get; set; }
    public int Category { get; set; }
    public string? CategoryName { get; set; }
    public int ItemType { get; set; }
    public string? ItemTypeName { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
}

/// <summary>
/// Cash Flow Summary DTO
/// </summary>
public class CashFlowSummaryDto
{
    public long OperatingCashFlow { get; set; }
    public long InvestingCashFlow { get; set; }
    public long FinancingCashFlow { get; set; }
    public long NetCashFlow { get; set; }
    public long BeginningBalance { get; set; }
    public long EndingBalance { get; set; }
}

/// <summary>
/// Cash Flow Forecast DTO
/// </summary>
public class CashFlowForecastDto
{
    public Guid Id { get; set; }
    public Guid? PeriodId { get; set; }
    public DateTime ForecastPeriod { get; set; }
    public long ProjectedCashFlow { get; set; }
    public int ConfidenceLevel { get; set; }
    public string? Assumptions { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to generate cash flow statement
/// </summary>
public class GenerateCashFlowRequest
{
    [Required] public Guid PeriodId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Operating Cash Flow Analysis DTO
/// </summary>
public class OperatingCashFlowDto
{
    public long Inflows { get; set; }
    public long Outflows { get; set; }
    public long NetOperatingCashFlow { get; set; }
}

/// <summary>
/// Investing Cash Flow Analysis DTO
/// </summary>
public class InvestingCashFlowDto
{
    public long Inflows { get; set; }
    public long Outflows { get; set; }
    public long NetInvestingCashFlow { get; set; }
}

/// <summary>
/// Financing Cash Flow Analysis DTO
/// </summary>
public class FinancingCashFlowDto
{
    public long Inflows { get; set; }
    public long Outflows { get; set; }
    public long NetFinancingCashFlow { get; set; }
}
