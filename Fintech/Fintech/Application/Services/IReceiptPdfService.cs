using System;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IReceiptPdfService
{
    /// <summary>
    /// Generate PDF for a single receipt with complete loan and customer details
    /// </summary>
    Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId);
    
    /// <summary>
    /// Generate PDFs for multiple receipts
    /// </summary>
    Task<byte[]> GenerateReceiptsPdfAsync(List<Guid> receiptIds);
    
    /// <summary>
    /// Get enhanced receipt details for display/PDF
    /// </summary>
    Task<EnhancedReceiptDto> GetEnhancedReceiptDetailsAsync(Guid receiptId);
}

public class EnhancedReceiptDto
{
    // Receipt Details
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    
    // Loan Details
    public string LoanId { get; set; } = string.Empty;
    public long LoanAmount { get; set; }
    public long PaidAmount { get; set; }
    public long RemainingAmount { get; set; }
    public long OverdueAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int PendingInstallments { get; set; }
    
    // Customer Details
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    
    // Payment Details
    public long CollectionAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string UTRRef { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string Remarks { get; set; } = string.Empty;
    
    // Company Details
    public string CompanyName { get; set; } = "FinVeda Microfinance";
    public string CompanyAddress { get; set; } = "Mumbai, India";
    public string CompanyPhone { get; set; } = "+91-XXXX-XXXX-XXXX";
    public string CompanyEmail { get; set; } = "info@finveda.com";
    public string CompanyRegistration { get; set; } = "REG-2024-001";
    
    // Branch Details
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    
    // Installment Details
    public List<InstallmentDetailDto> InstallmentDetails { get; set; } = new();
}

public class InstallmentDetailDto
{
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public long Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
    public long? PaidAmount { get; set; }
}
