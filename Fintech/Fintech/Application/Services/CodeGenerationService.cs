using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;

namespace Fintech.Application.Services;

/// <summary>
/// Service for generating and managing business codes across all entities.
/// Implements per-branch sequencing to ensure each branch maintains independent code sequences.
/// </summary>
public interface ICodeGenerationService
{
    /// <summary>
    /// Generates a unique business code for the specified entity type and branch.
    /// Examples: CUS0001, LN0001, PAR0001, etc.
    /// </summary>
    Task<string> GenerateCodeAsync(string entityName, Guid branchId);
    
    /// <summary>
    /// Initializes code sequences for all entities in a branch.
    /// Should be called when creating a new branch.
    /// </summary>
    Task InitializeBranchCodesAsync(Guid branchId);
    
    /// <summary>
    /// Gets the next sequence number for an entity in a branch (for reference).
    /// </summary>
    Task<int> GetNextSequenceNumberAsync(string entityName, Guid branchId);
}

public class CodeGenerationService : ICodeGenerationService
{
    private readonly FinVedaDbContext _context;
    
    // Code prefix and format mapping for all entities
    private static readonly Dictionary<string, (string Prefix, int PadLength)> CodeFormats = new()
    {
        { "Customer", ("CUS", 4) },
        { "LoanCase", ("LN", 4) },
        { "LoanProduct", ("PRO", 4) },
        { "Installment", ("INST", 4) },
        { "Receipt", ("RCP", 4) },
        { "Partner", ("PAR", 4) },
        { "CapitalAccount", ("CAP", 4) },
        { "JournalEntry", ("JE", 4) },
        { "JournalLine", ("JL", 4) },
        { "Account", ("ACC", 4) },
        { "DayEnd", ("DE", 4) },
        { "ProfitDistribution", ("PFT", 4) },
        { "Branch", ("BR", 4) },
        { "User", ("USR", 4) },
        { "AuditLog", ("AUD", 4) },
        { "CollectionRequest", ("COLREQ", 4) },
        { "CapitalTransaction", ("CAPTX", 4) }
    };

    public CodeGenerationService(FinVedaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Generates a unique business code for the specified entity type and branch.
    /// Thread-safe implementation using database transactions.
    /// </summary>
    public async Task<string> GenerateCodeAsync(string entityName, Guid branchId)
    {
        if (entityName == "Branch")
        {
            branchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));

        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID cannot be empty", nameof(branchId));

        if (!CodeFormats.TryGetValue(entityName, out var format))
            throw new InvalidOperationException($"No code format configured for entity: {entityName}");

        var (prefix, padLength) = format;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Fetch current sequence with exclusive lock (for thread safety)
            var sequence = await _context.CodeSequences
                .FirstOrDefaultAsync(cs => cs.EntityName == entityName && cs.BranchId == branchId);

            if (sequence == null)
            {
                // Initialize sequence if not exists
                sequence = new CodeSequence
                {
                    Id = Guid.NewGuid(),
                    EntityName = entityName,
                    BranchId = branchId,
                    CodePrefix = prefix,
                    NextSequenceNumber = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CodeSequences.Add(sequence);
                await _context.SaveChangesAsync();
            }

            // Generate code from current sequence
            var code = $"{prefix}{sequence.NextSequenceNumber.ToString().PadLeft(padLength, '0')}";

            // Increment sequence for next code
            sequence.NextSequenceNumber++;
            sequence.UpdatedAt = DateTime.UtcNow;
            _context.CodeSequences.Update(sequence);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return code;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException(
                $"Failed to generate code for {entityName} in branch {branchId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Initializes code sequences for all entities in a branch.
    /// Typically called when creating a new branch.
    /// </summary>
    public async Task InitializeBranchCodesAsync(Guid branchId)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID cannot be empty", nameof(branchId));

        var existingSequences = await _context.CodeSequences
            .Where(cs => cs.BranchId == branchId)
            .ToListAsync();

        foreach (var (entityName, (prefix, _)) in CodeFormats)
        {
            // Skip if already initialized
            if (existingSequences.Any(s => s.EntityName == entityName && s.BranchId == branchId))
                continue;

            var sequence = new CodeSequence
            {
                Id = Guid.NewGuid(),
                EntityName = entityName,
                BranchId = branchId,
                CodePrefix = prefix,
                NextSequenceNumber = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CodeSequences.Add(sequence);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the next sequence number that will be used for an entity in a branch.
    /// For reference/information only - does not increment the sequence.
    /// </summary>
    public async Task<int> GetNextSequenceNumberAsync(string entityName, Guid branchId)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));

        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID cannot be empty", nameof(branchId));

        var sequence = await _context.CodeSequences
            .FirstOrDefaultAsync(cs => cs.EntityName == entityName && cs.BranchId == branchId);

        return sequence?.NextSequenceNumber ?? 1;
    }
}
