using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Services;

public class ReceiptPdfService : IReceiptPdfService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;

    public ReceiptPdfService(IUnitOfWork unitOfWork, FinVedaDbContext dbContext)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId)
    {
        var receiptDetails = await GetEnhancedReceiptDetailsAsync(receiptId);
        return GeneratePdfContent(receiptDetails);
    }

    public async Task<byte[]> GenerateReceiptsPdfAsync(List<Guid> receiptIds)
    {
        var allReceipts = new List<EnhancedReceiptDto>();
        
        foreach (var receiptId in receiptIds)
        {
            var details = await GetEnhancedReceiptDetailsAsync(receiptId);
            allReceipts.Add(details);
        }

        return GenerateMultiplePdfContent(allReceipts);
    }

    public async Task<EnhancedReceiptDto> GetEnhancedReceiptDetailsAsync(Guid receiptId)
    {
        var receipt = await _dbContext.Receipts
            .Include(r => r.Installment)
            .Include(r => r.LoanCase)
            .ThenInclude(lc => lc.Customer)
            .FirstOrDefaultAsync(r => r.Id == receiptId);

        if (receipt == null)
        {
            throw new FinVedaException(404, "NOT_FOUND", "Receipt not found");
        }

        var installment = receipt.Installment;
        var loanCase = receipt.LoanCase;
        var customer = loanCase.Customer;

        // Get all installments for the loan
        var allInstallments = await _dbContext.Installments
            .Where(i => i.LoanCaseId == loanCase.Id)
            .OrderBy(i => i.No)
            .ToListAsync();

        var paidInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.paid);
        var pendingInstallments = allInstallments.Count(i => i.Status != InstallmentStatus.paid);

        // Calculate totals
        var totalPaid = await _dbContext.Receipts
            .Where(r => r.LoanCaseId == loanCase.Id)
            .SumAsync(r => r.AmountPaid);

        var remainingAmount = loanCase.TotalReceivable - totalPaid;

        // Get overdue amount
        var now = DateTime.UtcNow;
        var overdueInstallments = allInstallments
            .Where(i => i.Status != InstallmentStatus.paid && new DateTime(i.DueDate.Year, i.DueDate.Month, i.DueDate.Day) < now.Date)
            .ToList();
        var overdueAmount = overdueInstallments.Sum(i => i.Amount);

        // Get branch details
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == receipt.BranchId);

        // Build installment details
        var installmentDetails = allInstallments.Select(inst => new InstallmentDetailDto
        {
            InstallmentNo = inst.No,
            DueDate = inst.DueDate,
            Amount = inst.Amount,
            Status = inst.Status.ToString(),
            PaidDate = inst.Status == InstallmentStatus.paid ? DateTime.UtcNow : null,
            PaidAmount = inst.Status == InstallmentStatus.paid ? inst.Amount : null
        }).ToList();

        return new EnhancedReceiptDto
        {
            ReceiptNumber = receipt.PublicId,
            GeneratedDate = receipt.CapturedAt,
            
            LoanId = loanCase.Id.ToString(),
            LoanAmount = loanCase.Principal,
            PaidAmount = totalPaid,
            RemainingAmount = remainingAmount,
            OverdueAmount = overdueAmount,
            TotalInstallments = allInstallments.Count,
            PaidInstallments = paidInstallments,
            PendingInstallments = pendingInstallments,
            
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            CustomerAddress = "Address not available",
            CustomerId = customer.Id.ToString(),
            
            CollectionAmount = receipt.AmountPaid,
            PaymentMode = receipt.Mode.ToString(),
            UTRRef = receipt.UTRRef,
            PaymentDate = receipt.CapturedAt,
            Remarks = "",
            
            BranchName = branch?.Name ?? "Main Branch",
            BranchAddress = branch?.City ?? "Mumbai",
            
            InstallmentDetails = installmentDetails
        };
    }

    private byte[] GeneratePdfContent(EnhancedReceiptDto receipt)
    {
        using (var memoryStream = new MemoryStream())
        {
            // Note: This is a placeholder implementation
            // In production, use iTextSharp or similar library
            // For now, we'll create a simple text-based PDF representation
            
            var content = GenerateReceiptHtml(receipt);
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            
            // In production, convert HTML to PDF using HtmlToImage or similar
            // For now, return the HTML as bytes
            return bytes;
        }
    }

    private byte[] GenerateMultiplePdfContent(List<EnhancedReceiptDto> receipts)
    {
        using (var memoryStream = new MemoryStream())
        {
            var allContent = string.Join("\n\n---PAGE BREAK---\n\n", 
                receipts.Select(r => GenerateReceiptHtml(r)));
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(allContent);
            return bytes;
        }
    }

    private string GenerateReceiptHtml(EnhancedReceiptDto receipt)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
        .company-name {{ font-size: 24px; font-weight: bold; color: #333; }}
        .company-details {{ font-size: 12px; color: #666; margin-top: 5px; }}
        .receipt-title {{ font-size: 18px; font-weight: bold; margin-top: 20px; }}
        .receipt-number {{ font-size: 14px; color: #666; }}
        .section {{ margin: 20px 0; }}
        .section-title {{ font-size: 14px; font-weight: bold; background-color: #f0f0f0; padding: 10px; margin-bottom: 10px; }}
        .row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .label {{ font-weight: bold; width: 40%; }}
        .value {{ width: 60%; text-align: right; }}
        .amount {{ font-size: 16px; font-weight: bold; color: #2ecc71; }}
        .total-row {{ background-color: #f9f9f9; font-weight: bold; padding: 10px; margin-top: 10px; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; border-top: 1px solid #eee; padding-top: 20px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background-color: #f0f0f0; padding: 10px; text-align: left; border-bottom: 2px solid #333; }}
        td {{ padding: 8px; border-bottom: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='company-name'>{receipt.CompanyName}</div>
        <div class='company-details'>
            {receipt.CompanyAddress} | {receipt.CompanyPhone}<br/>
            {receipt.CompanyEmail} | Reg: {receipt.CompanyRegistration}
        </div>
    </div>

    <div style='text-align: center;'>
        <div class='receipt-title'>PAYMENT RECEIPT</div>
        <div class='receipt-number'>Receipt #: {receipt.ReceiptNumber}</div>
        <div class='receipt-number'>Date: {receipt.GeneratedDate:dd/MM/yyyy HH:mm:ss}</div>
    </div>

    <div class='section'>
        <div class='section-title'>CUSTOMER INFORMATION</div>
        <div class='row'>
            <div class='label'>Customer Name:</div>
            <div class='value'>{receipt.CustomerName}</div>
        </div>
        <div class='row'>
            <div class='label'>Customer ID:</div>
            <div class='value'>{receipt.CustomerId.Substring(0, 8)}...</div>
        </div>
        <div class='row'>
            <div class='label'>Phone:</div>
            <div class='value'>{receipt.CustomerPhone}</div>
        </div>
    </div>

    <div class='section'>
        <div class='section-title'>LOAN INFORMATION</div>
        <div class='row'>
            <div class='label'>Loan ID:</div>
            <div class='value'>{receipt.LoanId.Substring(0, 12)}...</div>
        </div>
        <div class='row'>
            <div class='label'>Loan Amount:</div>
            <div class='value amount'>₹{(receipt.LoanAmount / 100.0):N2}</div>
        </div>
        <div class='row'>
            <div class='label'>Total Paid:</div>
            <div class='value amount'>₹{(receipt.PaidAmount / 100.0):N2}</div>
        </div>
        <div class='row'>
            <div class='label'>Remaining Due:</div>
            <div class='value amount'>₹{(receipt.RemainingAmount / 100.0):N2}</div>
        </div>
        <div class='row'>
            <div class='label'>Overdue Amount:</div>
            <div class='value amount' style='color: #e74c3c;'>₹{(receipt.OverdueAmount / 100.0):N2}</div>
        </div>
    </div>

    <div class='section'>
        <div class='section-title'>INSTALLMENT SUMMARY</div>
        <div class='row'>
            <div class='label'>Total Installments:</div>
            <div class='value'>{receipt.TotalInstallments}</div>
        </div>
        <div class='row'>
            <div class='label'>Paid Installments:</div>
            <div class='value' style='color: #2ecc71;'>{receipt.PaidInstallments}</div>
        </div>
        <div class='row'>
            <div class='label'>Pending Installments:</div>
            <div class='value' style='color: #f39c12;'>{receipt.PendingInstallments}</div>
        </div>
    </div>

    <div class='section'>
        <div class='section-title'>PAYMENT DETAILS</div>
        <div class='row'>
            <div class='label'>Payment Amount:</div>
            <div class='value amount'>₹{(receipt.CollectionAmount / 100.0):N2}</div>
        </div>
        <div class='row'>
            <div class='label'>Payment Mode:</div>
            <div class='value'>{receipt.PaymentMode}</div>
        </div>
        <div class='row'>
            <div class='label'>Payment Date:</div>
            <div class='value'>{receipt.PaymentDate:dd/MM/yyyy}</div>
        </div>
        {(string.IsNullOrEmpty(receipt.UTRRef) ? "" : $@"
        <div class='row'>
            <div class='label'>UTR/Reference:</div>
            <div class='value'>{receipt.UTRRef}</div>
        </div>")}
    </div>

    <div class='section'>
        <div class='section-title'>INSTALLMENT DETAILS</div>
        <table>
            <thead>
                <tr>
                    <th>Inst. No</th>
                    <th>Due Date</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Paid Date</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", receipt.InstallmentDetails.Select(inst => $@"
                <tr>
                    <td>#{inst.InstallmentNo}</td>
                    <td>{inst.DueDate:dd/MM/yyyy}</td>
                    <td>₹{(inst.Amount / 100.0):N2}</td>
                    <td>{inst.Status}</td>
                    <td>{(inst.PaidDate.HasValue ? inst.PaidDate.Value.ToString("dd/MM/yyyy") : "-")}</td>
                </tr>"))}
            </tbody>
        </table>
    </div>

    <div class='footer'>
        <p>This is an electronically generated receipt. No signature required.</p>
        <p>For queries, contact: {receipt.CompanyEmail}</p>
        <p>Generated on: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}</p>
    </div>
</body>
</html>";
    }
}
