using Npgsql;
using System.Text;

class SeedDataVerifier
{
    private static string connectionString = "Host=localhost;Port=5432;Database=Fintech;Username=postgres;Password=Test123";
    private static List<string> alreadyPresent = new();
    private static List<string> inserted = new();
    private static List<string> errors = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("SEED DATA VERIFICATION STARTED");
        Console.WriteLine("========================================");
        Console.WriteLine();

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // 1. BRANCHES
            await VerifyBranches(conn);

            // 2. USERS
            await VerifyUsers(conn);

            // 3. ACCOUNTS
            await VerifyAccounts(conn);

            // 4. CUSTOMERS
            await VerifyCustomers(conn);

            // 5. LOAN PRODUCTS
            await VerifyLoanProducts(conn);

            // 6. PARTNERS
            await VerifyPartners(conn);

            // 7. CAPITAL ACCOUNTS
            await VerifyCapitalAccounts(conn);

            // 8. LOAN CASES
            await VerifyLoanCases(conn);

            // 9. INSTALLMENTS
            await VerifyInstallments(conn);

            // 10. RECEIPTS
            await VerifyReceipts(conn);

            // 11. JOURNAL ENTRIES
            await VerifyJournalEntries(conn);

            // 12. JOURNAL LINES
            await VerifyJournalLines(conn);

            // 13. AUDIT LOGS
            await VerifyAuditLogs(conn);

            // 14. DAY ENDS
            await VerifyDayEnds(conn);

            // 15. PROFIT DISTRIBUTIONS
            await VerifyProfitDistributions(conn);

            // Print Summary
            PrintSummary();

            // Save Report
            SaveReport();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL ERROR] {ex.Message}");
            errors.Add($"Fatal: {ex.Message}");
        }
    }

    static async Task<bool> RecordExists(NpgsqlConnection conn, string table, string idColumn, string idValue)
    {
        var query = $"SELECT COUNT(*) FROM {table} WHERE {idColumn} = @id";
        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("id", Guid.Parse(idValue));
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }

    static async Task InsertRecord(NpgsqlConnection conn, string table, string query, string description)
    {
        try
        {
            await using var cmd = new NpgsqlCommand(query, conn);
            await cmd.ExecuteNonQueryAsync();
            inserted.Add($"{table} - {description}");
            Console.WriteLine($"[INSERTED] {table} - {description}");
        }
        catch (Exception ex)
        {
            errors.Add($"{table} - {description}: {ex.Message}");
            Console.WriteLine($"[ERROR] {table} - {description}: {ex.Message}");
        }
    }

    static async Task VerifyBranches(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking BRANCHES...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        
        if (await RecordExists(conn, "branches", "id", branchId))
        {
            alreadyPresent.Add("branches - FinVeda Main Office");
            Console.WriteLine("[EXISTS] branches - FinVeda Main Office");
        }
        else
        {
            var query = $@"
                INSERT INTO branches (id, name, city, is_active, settings_json) 
                VALUES ('{branchId}', 'FinVeda Main Office', 'Mumbai', true, '{{}}')";
            await InsertRecord(conn, "branches", query, "FinVeda Main Office");
        }
    }

    static async Task VerifyUsers(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking USERS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        
        var users = new[]
        {
            new { Id = "22222222-2222-2222-2222-222222222221", Name = "Super Admin User", Email = "super_admin@finveda.com", Role = "super_admin" },
            new { Id = "22222222-2222-2222-2222-222222222222", Name = "Branch Manager User", Email = "branch_manager@finveda.com", Role = "branch_manager" },
            new { Id = "22222222-2222-2222-2222-222222222223", Name = "Partner User", Email = "partner@finveda.com", Role = "partner" },
            new { Id = "22222222-2222-2222-2222-222222222224", Name = "Accountant User", Email = "accountant@finveda.com", Role = "accountant" },
            new { Id = "22222222-2222-2222-2222-222222222225", Name = "Collection Officer User", Email = "collection_officer@finveda.com", Role = "collection_officer" },
            new { Id = "22222222-2222-2222-2222-222222222226", Name = "Recovery Specialist User", Email = "recovery_specialist@finveda.com", Role = "recovery_specialist" },
            new { Id = "22222222-2222-2222-2222-222222222227", Name = "Loan Officer User", Email = "loan_officer@finveda.com", Role = "loan_officer" },
            new { Id = "22222222-2222-2222-2222-222222222228", Name = "Customer User", Email = "customer@finveda.com", Role = "customer" },
            new { Id = "22222222-2222-2222-2222-222222222229", Name = "Agent User", Email = "agent@finveda.com", Role = "agent" }
        };

        foreach (var user in users)
        {
            if (await RecordExists(conn, "users", "id", user.Id))
            {
                alreadyPresent.Add($"users - {user.Name}");
                Console.WriteLine($"[EXISTS] users - {user.Name}");
            }
            else
            {
                var query = $@"
                    INSERT INTO users (id, branch_id, name, email, password_hash, role, is_active, totp_enabled, refresh_token) 
                    VALUES ('{user.Id}', '{branchId}', '{user.Name}', '{user.Email}', 'Admin@123', '{user.Role}', true, false, '')";
                await InsertRecord(conn, "users", query, user.Name);
            }
        }
    }

    static async Task VerifyAccounts(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking ACCOUNTS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        
        var accounts = new[]
        {
            new { Id = "33333333-3333-3333-3333-333333333331", Name = "Cash Office" },
            new { Id = "33333333-3333-3333-3333-333333333332", Name = "HDFC Bank Account" },
            new { Id = "33333333-3333-3333-3333-333333333333", Name = "Loan Portfolio" },
            new { Id = "33333333-3333-3333-3333-333333333334", Name = "Interest Revenue" },
            new { Id = "33333333-3333-3333-3333-333333333335", Name = "Equity Capital" }
        };

        foreach (var account in accounts)
        {
            if (await RecordExists(conn, "accounts", "id", account.Id))
            {
                alreadyPresent.Add($"accounts - {account.Name}");
                Console.WriteLine($"[EXISTS] accounts - {account.Name}");
            }
            else
            {
                var query = $@"
                    INSERT INTO accounts (id, branch_id, name) 
                    VALUES ('{account.Id}', '{branchId}', '{account.Name}')";
                await InsertRecord(conn, "accounts", query, account.Name);
            }
        }
    }

    static async Task VerifyCustomers(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking CUSTOMERS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        
        var customers = new[]
        {
            new { Id = "44444444-4444-4444-4444-444444444441", Name = "Amit Sharma", Phone = "9876543210", Aadhaar = "ENCRYPTED_AD123", Pan = "ENCRYPTED_PAN123" },
            new { Id = "44444444-4444-4444-4444-444444444442", Name = "Meera Patel", Phone = "9876543211", Aadhaar = "ENCRYPTED_AD456", Pan = "ENCRYPTED_PAN456" }
        };

        foreach (var customer in customers)
        {
            if (await RecordExists(conn, "customers", "id", customer.Id))
            {
                alreadyPresent.Add($"customers - {customer.Name}");
                Console.WriteLine($"[EXISTS] customers - {customer.Name}");
            }
            else
            {
                var query = $@"
                    INSERT INTO customers (id, branch_id, name, phone, is_active, aadhaar_encrypted, pan_encrypted) 
                    VALUES ('{customer.Id}', '{branchId}', '{customer.Name}', '{customer.Phone}', true, '{customer.Aadhaar}', '{customer.Pan}')";
                await InsertRecord(conn, "customers", query, customer.Name);
            }
        }
    }

    static async Task VerifyLoanProducts(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking LOAN PRODUCTS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        
        var products = new[]
        {
            new { Id = "55555555-5555-5555-5555-555555555551", Name = "Personal Loan", Frequency = "monthly", MinAmount = 1000000, MaxAmount = 50000000, InterestRate = 12.0 },
            new { Id = "55555555-5555-5555-5555-555555555552", Name = "Biz Loan", Frequency = "weekly", MinAmount = 5000000, MaxAmount = 250000000, InterestRate = 18.0 }
        };

        foreach (var product in products)
        {
            if (await RecordExists(conn, "loan_products", "id", product.Id))
            {
                alreadyPresent.Add($"loan_products - {product.Name}");
                Console.WriteLine($"[EXISTS] loan_products - {product.Name}");
            }
            else
            {
                var query = $@"
                    INSERT INTO loan_products (id, branch_id, name, frequency, min_amount, max_amount, interest_rate_pct, is_active) 
                    VALUES ('{product.Id}', '{branchId}', '{product.Name}', '{product.Frequency}', {product.MinAmount}, {product.MaxAmount}, {product.InterestRate}, true)";
                await InsertRecord(conn, "loan_products", query, product.Name);
            }
        }
    }

    static async Task VerifyPartners(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking PARTNERS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var partnerId = "66666666-6666-6666-6666-666666666661";
        var partnerUserId = "22222222-2222-2222-2222-222222222223";
        
        if (await RecordExists(conn, "partners", "id", partnerId))
        {
            alreadyPresent.Add("partners - Partner User (25% equity)");
            Console.WriteLine("[EXISTS] partners - Partner User (25% equity)");
        }
        else
        {
            var query = $@"
                INSERT INTO partners (id, branch_id, user_id, equity_pct, is_active) 
                VALUES ('{partnerId}', '{branchId}', '{partnerUserId}', 25.0, true)";
            await InsertRecord(conn, "partners", query, "Partner User (25% equity)");
        }
    }

    static async Task VerifyCapitalAccounts(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking CAPITAL ACCOUNTS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var capitalAccountId = "77777777-7777-7777-7777-777777777771";
        var partnerId = "66666666-6666-6666-6666-666666666661";
        
        if (await RecordExists(conn, "capital_accounts", "id", capitalAccountId))
        {
            alreadyPresent.Add("capital_accounts - Investment 500000000");
            Console.WriteLine("[EXISTS] capital_accounts - Investment 500000000");
        }
        else
        {
            var query = $@"
                INSERT INTO capital_accounts (id, branch_id, partner_id, amount, transaction_type, created_at) 
                VALUES ('{capitalAccountId}', '{branchId}', '{partnerId}', 500000000, 'investment', NOW())";
            await InsertRecord(conn, "capital_accounts", query, "Investment 500000000");
        }
    }

    static async Task VerifyLoanCases(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking LOAN CASES...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var loanCaseId = "88888888-8888-8888-8888-888888888881";
        var customerId = "44444444-4444-4444-4444-444444444441";
        
        if (await RecordExists(conn, "loan_cases", "id", loanCaseId))
        {
            alreadyPresent.Add("loan_cases - Loan Case 5000000");
            Console.WriteLine("[EXISTS] loan_cases - Loan Case 5000000");
        }
        else
        {
            var query = $@"
                INSERT INTO loan_cases (id, branch_id, customer_id, finance_amount, interest_amount, total_receivable, file_charges_amount, status, version) 
                VALUES ('{loanCaseId}', '{branchId}', '{customerId}', 5000000, 600000, 5600000, 50000, 'active', 1)";
            await InsertRecord(conn, "loan_cases", query, "Loan Case 5000000");
        }
    }

    static async Task VerifyInstallments(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking INSTALLMENTS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var loanCaseId = "88888888-8888-8888-8888-888888888881";
        
        var installments = new[]
        {
            new { Id = "99999999-9999-9999-9999-999999999991", No = 1, Amount = 933333, Status = "paid", DueDate = "NOW() - INTERVAL '1 month'" },
            new { Id = "99999999-9999-9999-9999-999999999992", No = 2, Amount = 933333, Status = "pending", DueDate = "NOW()" }
        };

        foreach (var installment in installments)
        {
            if (await RecordExists(conn, "installments", "id", installment.Id))
            {
                alreadyPresent.Add($"installments - Installment {installment.No}");
                Console.WriteLine($"[EXISTS] installments - Installment {installment.No}");
            }
            else
            {
                var query = $@"
                    INSERT INTO installments (id, branch_id, loan_case_id, installment_no, due_date, amount, status) 
                    VALUES ('{installment.Id}', '{branchId}', '{loanCaseId}', {installment.No}, {installment.DueDate}, {installment.Amount}, '{installment.Status}')";
                await InsertRecord(conn, "installments", query, $"Installment {installment.No}");
            }
        }
    }

    static async Task VerifyReceipts(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking RECEIPTS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var receiptId = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA1";
        var installmentId = "99999999-9999-9999-9999-999999999991";
        var loanCaseId = "88888888-8888-8888-8888-888888888881";
        
        if (await RecordExists(conn, "receipts", "id", receiptId))
        {
            alreadyPresent.Add("receipts - RCP-1001");
            Console.WriteLine("[EXISTS] receipts - RCP-1001");
        }
        else
        {
            var query = $@"
                INSERT INTO receipts (id, branch_id, public_id, installment_id, loan_case_id, amount_paid, mode, utr_ref, captured_at) 
                VALUES ('{receiptId}', '{branchId}', 'RCP-1001', '{installmentId}', '{loanCaseId}', 933333, 'cash', 'CASH001', NOW() - INTERVAL '1 month')";
            await InsertRecord(conn, "receipts", query, "RCP-1001");
        }
    }

    static async Task VerifyJournalEntries(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking JOURNAL ENTRIES...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var journalEntryId = "BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBB1";
        
        if (await RecordExists(conn, "journal_entries", "id", journalEntryId))
        {
            alreadyPresent.Add("journal_entries - JE-3001");
            Console.WriteLine("[EXISTS] journal_entries - JE-3001");
        }
        else
        {
            var query = $@"
                INSERT INTO journal_entries (id, branch_id, public_id, entry_date, description, reference, is_manual, is_posted) 
                VALUES ('{journalEntryId}', '{branchId}', 'JE-3001', NOW(), 'Initial Capital Injection', 'VC-01', true, true)";
            await InsertRecord(conn, "journal_entries", query, "JE-3001");
        }
    }

    static async Task VerifyJournalLines(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking JOURNAL LINES...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var journalEntryId = "BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBB1";
        
        var lines = new[]
        {
            new { Id = "CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCC1", AccountName = "Cash Office", EntryType = "debit", Amount = 500000000 },
            new { Id = "CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCC2", AccountName = "Equity Capital", EntryType = "credit", Amount = 500000000 }
        };

        foreach (var line in lines)
        {
            if (await RecordExists(conn, "journal_lines", "id", line.Id))
            {
                alreadyPresent.Add($"journal_lines - {line.AccountName} {line.EntryType}");
                Console.WriteLine($"[EXISTS] journal_lines - {line.AccountName} {line.EntryType}");
            }
            else
            {
                var query = $@"
                    INSERT INTO journal_lines (id, branch_id, journal_entry_id, account_name, entry_type, amount) 
                    VALUES ('{line.Id}', '{branchId}', '{journalEntryId}', '{line.AccountName}', '{line.EntryType}', {line.Amount})";
                await InsertRecord(conn, "journal_lines", query, $"{line.AccountName} {line.EntryType}");
            }
        }
    }

    static async Task VerifyAuditLogs(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking AUDIT LOGS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var auditLogId = "DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDD1";
        
        if (await RecordExists(conn, "audit_logs", "id", auditLogId))
        {
            alreadyPresent.Add("audit_logs - Initial Setup");
            Console.WriteLine("[EXISTS] audit_logs - Initial Setup");
        }
        else
        {
            var query = $@"
                INSERT INTO audit_logs (id, branch_id, table_name, record_id, action, before_val, after_val, timestamp) 
                VALUES ('{auditLogId}', '{branchId}', 'branches', '{branchId}', 'INSERT', '{{}}', '{{""name"": ""Initial Setup""}}', NOW())";
            await InsertRecord(conn, "audit_logs", query, "Initial Setup");
        }
    }

    static async Task VerifyDayEnds(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking DAY ENDS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var dayEndId = "EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEE1";
        
        if (await RecordExists(conn, "day_ends", "id", dayEndId))
        {
            alreadyPresent.Add("day_ends - Day End Record");
            Console.WriteLine("[EXISTS] day_ends - Day End Record");
        }
        else
        {
            var query = $@"
                INSERT INTO day_ends (id, branch_id, date, is_closed, journals_locked, total_collected, discrepancy, discrepancy_resolved) 
                VALUES ('{dayEndId}', '{branchId}', NOW() - INTERVAL '1 day', true, 12, 4500000, 0, true)";
            await InsertRecord(conn, "day_ends", query, "Day End Record");
        }
    }

    static async Task VerifyProfitDistributions(NpgsqlConnection conn)
    {
        Console.WriteLine("Checking PROFIT DISTRIBUTIONS...");
        var branchId = "11111111-1111-1111-1111-111111111111";
        var profitDistId = "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFF1";
        var partnerId = "66666666-6666-6666-6666-666666666661";
        
        if (await RecordExists(conn, "profit_distributions", "id", profitDistId))
        {
            alreadyPresent.Add("profit_distributions - March 2026");
            Console.WriteLine("[EXISTS] profit_distributions - March 2026");
        }
        else
        {
            var query = $@"
                INSERT INTO profit_distributions (id, branch_id, period, payout_amount, status, processed_at, partner_id) 
                VALUES ('{profitDistId}', '{branchId}', 'March 2026', 2500000, 'processed', NOW(), '{partnerId}')";
            await InsertRecord(conn, "profit_distributions", query, "March 2026");
        }
    }

    static void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("SEED DATA VERIFICATION SUMMARY");
        Console.WriteLine("========================================");
        Console.WriteLine();

        Console.WriteLine("ALREADY PRESENT IN DATABASE:");
        Console.WriteLine($"Total: {alreadyPresent.Count}");
        foreach (var item in alreadyPresent)
        {
            Console.WriteLine($"  [OK] {item}");
        }

        Console.WriteLine();
        Console.WriteLine("NEWLY INSERTED RECORDS:");
        Console.WriteLine($"Total: {inserted.Count}");
        foreach (var item in inserted)
        {
            Console.WriteLine($"  [NEW] {item}");
        }

        Console.WriteLine();
        Console.WriteLine("ERRORS ENCOUNTERED:");
        Console.WriteLine($"Total: {errors.Count}");
        foreach (var item in errors)
        {
            Console.WriteLine($"  [ERR] {item}");
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("VERIFICATION COMPLETE");
        Console.WriteLine("========================================");
    }

    static void SaveReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("SEED DATA VERIFICATION REPORT");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("Database: Fintech at localhost:5432");
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine("SUMMARY");
        sb.AppendLine("========================================");
        sb.AppendLine($"Already Present: {alreadyPresent.Count}");
        sb.AppendLine($"Newly Inserted: {inserted.Count}");
        sb.AppendLine($"Errors: {errors.Count}");
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine("ALREADY PRESENT IN DATABASE");
        sb.AppendLine("========================================");
        foreach (var item in alreadyPresent)
        {
            sb.AppendLine($"  [OK] {item}");
        }
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine("NEWLY INSERTED RECORDS");
        sb.AppendLine("========================================");
        foreach (var item in inserted)
        {
            sb.AppendLine($"  [NEW] {item}");
        }
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine("ERRORS ENCOUNTERED");
        sb.AppendLine("========================================");
        foreach (var item in errors)
        {
            sb.AppendLine($"  [ERROR] {item}");
        }

        File.WriteAllText("seed_data_verification_report.txt", sb.ToString());
        Console.WriteLine();
        Console.WriteLine("Detailed report saved to: seed_data_verification_report.txt");
    }
}
