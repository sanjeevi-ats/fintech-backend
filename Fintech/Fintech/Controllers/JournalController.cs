using Fintech.Application.Services;
using Fintech.Application.DTOs;
using Fintech.Infrastructure.Persistence;
using Fintech.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
public class JournalController : BaseApiController
{
    private readonly FinVedaDbContext _dbContext;
    private readonly ILogger<JournalController> _logger;
    private readonly IJournalService _journalService;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;

    public JournalController(
        FinVedaDbContext dbContext,
        ILogger<JournalController> logger,
        IJournalService journalService,
        ICodeGenerationService codeService,
        ITenantService tenantService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _journalService = journalService;
        _codeService = codeService;
        _tenantService = tenantService;
    }

    /// <summary>
    /// GET /api/v1/journal/entries
    /// Returns paginated list of journal entries with filtering and sorting
    /// Query params: startDate, endDate, transactionType, limit (50), offset (0)
    /// </summary>
    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? transactionType,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            var branchId = BranchId;
            if (branchId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            // Validate pagination parameters
            if (limit <= 0 || limit > 500)
                limit = 50;
            if (offset < 0)
                offset = 0;

            var query = _dbContext.JournalEntries
                .Include(je => je.Lines)
                .Where(je => je.BranchId == branchId);

            // Apply date range filter
            if (startDate.HasValue)
                query = query.Where(je => je.Date >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(je => je.Date <= endDate.Value);

            // Apply transaction type filter (manual vs automatic)
            if (!string.IsNullOrEmpty(transactionType))
            {
                bool isManual = transactionType.Equals("manual", StringComparison.OrdinalIgnoreCase);
                query = query.Where(je => je.IsManual == isManual);
            }

            var total = await query.CountAsync();

            var entries = await query
                .OrderByDescending(je => je.Date)
                .ThenByDescending(je => je.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(je => new
                {
                    je.Id,
                    je.JournalEntryCode,
                    je.PublicId,
                    je.Date,
                    je.Description,
                    je.Reference,
                    je.IsManual,
                    je.IsPosted,
                    CapitalTransactionId = je.CapitalTransactionId,
                    Lines = je.Lines.Select(l => new
                    {
                        l.JournalLineCode,
                        l.AccountCode,
                        l.AccountName,
                        l.Type,
                        l.Amount
                    })
                })
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                Data = entries,
                Pagination = new { Total = total, Limit = limit, Offset = offset, Returned = entries.Count }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEntries");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving journal entries.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/v1/journal/entries/{id}
    /// Returns detailed journal entry with both debit and credit lines and audit trail
    /// Route params: id (journal entry ID)
    /// </summary>
    [HttpGet("entries/{id}")]
    public async Task<IActionResult> GetEntryById(Guid id)
    {
        try
        {
            var branchId = BranchId;
            if (branchId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            var entry = await _dbContext.JournalEntries
                .Include(je => je.Lines)
                .FirstOrDefaultAsync(je => je.Id == id && je.BranchId == branchId);

            if (entry == null)
                return NotFound(new { Success = false, Message = "Journal entry not found" });

            var debits = entry.Lines.Where(l => l.Type.ToLower() == "debit").ToList();
            var credits = entry.Lines.Where(l => l.Type.ToLower() == "credit").ToList();

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    entry.Id,
                    entry.JournalEntryCode,
                    entry.PublicId,
                    entry.Date,
                    entry.Description,
                    entry.Reference,
                    entry.IsManual,
                    entry.IsPosted,
                    entry.CapitalTransactionId,
                    entry.CreatedBy,
                    entry.CreatedAt,
                    Debits = debits.Select(l => new
                    {
                        l.JournalLineCode,
                        l.AccountCode,
                        l.AccountName,
                        l.Amount
                    }),
                    Credits = credits.Select(l => new
                    {
                        l.JournalLineCode,
                        l.AccountCode,
                        l.AccountName,
                        l.Amount
                    }),
                    TotalDebits = debits.Sum(d => d.Amount),
                    TotalCredits = credits.Sum(c => c.Amount),
                    IsBalanced = debits.Sum(d => d.Amount) == credits.Sum(c => c.Amount)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEntryById");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving journal entry.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// POST /api/v1/journal/entries
    /// Creates a manual journal entry (requires ManualJournalEntry permission)
    /// Body: { lines: [...], description: string }
    /// </summary>
    [HttpPost("entries")]
    public async Task<IActionResult> CreateManualEntry([FromBody] CreateManualJournalEntryRequest request)
    {
        try
        {
            var branchId = BranchId;
            var userId = UserId;

            if (branchId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            if (request?.Lines == null || request.Lines.Count < 2)
                return BadRequest(new { Success = false, Message = "Manual entries must have at least 2 lines (debit and credit)" });

            if (string.IsNullOrWhiteSpace(request.Description))
                return BadRequest(new { Success = false, Message = "Description is required" });

            // Validate debits = credits
            var totalDebits = request.Lines
                .Where(l => l.Type?.ToLower() == "debit")
                .Sum(l => l.Amount);
            var totalCredits = request.Lines
                .Where(l => l.Type?.ToLower() == "credit")
                .Sum(l => l.Amount);

            if (totalDebits != totalCredits)
                return BadRequest(new { Success = false, Message = $"Debits ({totalDebits}) must equal credits ({totalCredits})" });

            var lines = request.Lines.Select(l => 
                (l.AccountCode, l.AccountName, l.Type, l.Amount)
            ).ToList();

            var entry = await _journalService.CreateManualJournalEntryAsync(
                lines,
                request.Description,
                userId);

            return CreatedAtAction(nameof(GetEntryById), new { id = entry.Id }, new
            {
                Success = true,
                Data = new
                {
                    entry.Id,
                    entry.JournalEntryCode,
                    entry.Date,
                    entry.Description,
                    entry.IsManual,
                    entry.IsPosted,
                    Message = "Manual journal entry created and pending approval"
                }
            });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateManualEntry");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while creating manual journal entry.",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// DELETE /api/v1/journal/entries/{id}
    /// Soft deletes a journal entry and creates reversal entry (requires supervisor approval)
    /// Route params: id (journal entry ID)
    /// </summary>
    [HttpDelete("entries/{id}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        try
        {
            var branchId = BranchId;
            var userId = UserId;

            if (branchId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "BranchId is required" });

            var entry = await _dbContext.JournalEntries
                .Include(je => je.Lines)
                .FirstOrDefaultAsync(je => je.Id == id && je.BranchId == branchId);

            if (entry == null)
                return NotFound(new { Success = false, Message = "Journal entry not found" });

            // Create reversal entry
            var reversalEntry = await _journalService.ReverseJournalEntryAsync(id, userId);

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    OriginalEntryCode = entry.JournalEntryCode,
                    ReversalEntryCode = reversalEntry.JournalEntryCode,
                    ReversalDate = reversalEntry.Date,
                    Message = "Entry reversed successfully (soft delete with reversal entry created)"
                }
            });
        }
        catch (FinVedaException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteEntry");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while deleting journal entry.",
                Error = ex.Message
            });
        }
    }

    // ===== LEGACY ENDPOINTS (kept for backward compatibility) =====

    [HttpGet("entries/by-code/{code}")]
    public async Task<IActionResult> GetEntryByCode(string code)
    {
        try
        {
            var entry = await _dbContext.JournalEntries
                .Include(je => je.Lines)
                .FirstOrDefaultAsync(je => je.JournalEntryCode == code && je.BranchId == _tenantService.BranchId);

            if (entry == null)
                return NotFound(new { message = "Journal entry not found" });

            return Ok(new
            {
                entry.Id,
                entry.JournalEntryCode,
                entry.PublicId,
                entry.Date,
                entry.Description,
                entry.Reference,
                Lines = entry.Lines.Select(l => new
                {
                    l.JournalLineCode,
                    l.AccountName,
                    l.Type,
                    l.Amount
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEntryByCode");
            return StatusCode(500, new { message = "Error retrieving journal entry" });
        }
    }

    [HttpGet("lines/by-code/{code}")]
    public async Task<IActionResult> GetLineByCode(string code)
    {
        try
        {
            var line = await _dbContext.JournalLines
                .FirstOrDefaultAsync(jl => jl.JournalLineCode == code && jl.BranchId == _tenantService.BranchId);

            if (line == null)
                return NotFound(new { message = "Journal line not found" });

            return Ok(new
            {
                line.Id,
                line.JournalLineCode,
                line.JournalEntryId,
                line.AccountName,
                line.Type,
                line.Amount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLineByCode");
            return StatusCode(500, new { message = "Error retrieving journal line" });
        }
    }
}

/// <summary>
/// DTO for creating manual journal entries
/// </summary>
public class CreateManualJournalEntryRequest
{
    public List<CreateManualJournalEntryLineRequest> Lines { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

public class CreateManualJournalEntryLineRequest
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;  // debit or credit
    public long Amount { get; set; }
}
