-- FINVEDA MASTER SEEDING SCRIPT (POSTGRESQL) --
-- Use this script to manually populate your 15+ database tables --

-- 1. CLEANUP (OPTIONAL - USE WITH CAUTION) --
-- DELETE FROM audit_logs; DELETE FROM receipts; DELETE FROM installments; DELETE FROM loan_cases; DELETE FROM capital_accounts; DELETE FROM partners; DELETE FROM journal_lines; DELETE FROM journal_entries; DELETE FROM accounts; DELETE FROM customers; DELETE FROM loan_products; DELETE FROM users; DELETE FROM branches;

-- 2. BRANCHES (BRANCH-001) --
ALTER TABLE branches DISABLE TRIGGER ALL;
INSERT INTO branches (id, name, city, is_active, settings_json, branch_code) VALUES 
('11111111-1111-1111-1111-111111111111', 'FinVeda Main Office', 'Mumbai', true, '{}', 'BR0001');
ALTER TABLE branches ENABLE TRIGGER ALL;

-- 3. USERS (PASSWORDS: Admin@123) --
-- Using plain text for now (LoginUser handler has fallback support)
INSERT INTO users (id, branch_id, name, email, password_hash, role, is_active, totp_enabled, refresh_token, user_code) VALUES
('22222222-2222-2222-2222-222222222221', '11111111-1111-1111-1111-111111111111', 'Super Admin User', 'super_admin@finveda.com', 'Admin@123', 'super_admin', true, false, '', 'USR0001'),
('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'Branch Manager User', 'branch_manager@finveda.com', 'Admin@123', 'branch_manager', true, false, '', 'USR0002'),
('22222222-2222-2222-2222-222222222223', '11111111-1111-1111-1111-111111111111', 'Partner User', 'partner@finveda.com', 'Admin@123', 'partner', true, false, '', 'USR0003'),
('22222222-2222-2222-2222-222222222224', '11111111-1111-1111-1111-111111111111', 'Accountant User', 'accountant@finveda.com', 'Admin@123', 'accountant', true, false, '', 'USR0004'),
('22222222-2222-2222-2222-222222222225', '11111111-1111-1111-1111-111111111111', 'Collection Officer User', 'collection_officer@finveda.com', 'Admin@123', 'collection_officer', true, false, '', 'USR0005'),
('22222222-2222-2222-2222-222222222226', '11111111-1111-1111-1111-111111111111', 'Recovery Specialist User', 'recovery_specialist@finveda.com', 'Admin@123', 'recovery_specialist', true, false, '', 'USR0006'),
('22222222-2222-2222-2222-222222222227', '11111111-1111-1111-1111-111111111111', 'Loan Officer User', 'loan_officer@finveda.com', 'Admin@123', 'loan_officer', true, false, '', 'USR0007'),
('22222222-2222-2222-2222-222222222228', '11111111-1111-1111-1111-111111111111', 'Customer User', 'customer@finveda.com', 'Admin@123', 'customer', true, false, '', 'USR0008'),
('22222222-2222-2222-2222-222222222229', '11111111-1111-1111-1111-111111111111', 'Agent User', 'agent@finveda.com', 'Admin@123', 'agent', true, false, '', 'USR0009');

-- 4. ACCOUNTS (Chart of Accounts) --
INSERT INTO accounts (id, branch_id, name, account_code) VALUES
('33333333-3333-3333-3333-333333333330', '11111111-1111-1111-1111-111111111111', 'Bank/Asset', 'GL-1000'),
('33333333-3333-3333-3333-333333333331', '11111111-1111-1111-1111-111111111111', 'Cash Office', 'GL-1001'),
('33333333-3333-3333-3333-333333333332', '11111111-1111-1111-1111-111111111111', 'HDFC Bank Account', 'GL-1002'),
('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'Loan Portfolio', 'GL-1200'),
('33333333-3333-3333-3333-333333333334', '11111111-1111-1111-1111-111111111111', 'Interest Revenue', 'GL-4100'),
('33333333-3333-3333-3333-333333333335', '11111111-1111-1111-1111-111111111111', 'Equity Capital', 'GL-3000'),
('33333333-3333-3333-3333-333333333336', '11111111-1111-1111-1111-111111111111', 'Retained Earnings', 'GL-3100');

-- 5. CUSTOMERS --
INSERT INTO customers (id, branch_id, name, phone, is_active, aadhaar_encrypted, pan_encrypted, customer_code) VALUES
('44444444-4444-4444-4444-444444444441', '11111111-1111-1111-1111-111111111111', 'Amit Sharma', '9876543210', true, 'ENCRYPTED_AD123', 'ENCRYPTED_PAN123', 'CUS0001'),
('44444444-4444-4444-4444-444444444442', '11111111-1111-1111-1111-111111111111', 'Meera Patel', '9876543211', true, 'ENCRYPTED_AD456', 'ENCRYPTED_PAN456', 'CUS0002');

-- 6. LOAN PRODUCTS --
INSERT INTO loan_products (id, branch_id, name, repayment_frequency, interest_rate, default_tenure_months, is_active, code) VALUES
('55555555-5555-5555-5555-555555555551', '11111111-1111-1111-1111-111111111111', 'Personal Loan', 'monthly', 12.0, 12, true, 'LP0001'),
('55555555-5555-5555-5555-555555555552', '11111111-1111-1111-1111-111111111111', 'Biz Loan', 'weekly', 18.0, 24, true, 'LP0002');

-- 7. PARTNERS --
INSERT INTO partners (id, branch_id, user_id, equity_pct, is_active, partner_code) VALUES
('66666666-6666-6666-6666-666666666661', '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222223', 25.0, true, 'PAR0001');

-- 8. CAPITAL ACCOUNTS --
INSERT INTO capital_accounts (id, partner_id, opening_balance, current_balance, ownership_percentage, status, currency, created_by, created_at, capital_account_code) VALUES
('77777777-7777-7777-7777-777777777771', '66666666-6666-6666-6666-666666666661', 500000000, 500000000, 25.0, 'Active', 'INR', '22222222-2222-2222-2222-222222222221', NOW(), 'CAP0001');

-- 9. LOAN CASES --
INSERT INTO loan_cases (id, branch_id, customer_id, finance_amount, interest_amount, total_receivable, file_charges_amount, status, version, loan_code) VALUES
('88888888-8888-8888-8888-888888888881', '11111111-1111-1111-1111-111111111111', '44444444-4444-4444-4444-444444444441', 5000000, 600000, 5600000, 50000, 'active', 1, 'LN00001');

-- 10. INSTALLMENTS --
INSERT INTO installments (id, branch_id, loan_case_id, installment_no, due_date, amount, status, installment_code) VALUES
('99999999-9999-9999-9999-999999999991', '11111111-1111-1111-1111-111111111111', '88888888-8888-8888-8888-888888888881', 1, NOW() - INTERVAL '1 month', 933333, 'paid', 'INS0001'),
('99999999-9999-9999-9999-999999999992', '11111111-1111-1111-1111-111111111111', '88888888-8888-8888-8888-888888888881', 2, NOW(), 933333, 'pending', 'INS0002');

-- 11. RECEIPTS --
INSERT INTO receipts (id, branch_id, receipt_code, installment_id, loan_case_id, amount_paid, mode, utr_ref, captured_at, remarks, public_id) VALUES
('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA1', '11111111-1111-1111-1111-111111111111', 'RCP-1001', '99999999-9999-9999-9999-999999999991', '88888888-8888-8888-8888-888888888881', 933333, 'cash', 'CASH001', NOW() - INTERVAL '1 month', 'Seeded receipt', 'RCP-1001');

-- 12. JOURNAL ENTRIES --
INSERT INTO journal_entries (id, branch_id, journal_entry_code, entry_date, description, reference, is_manual, is_posted, created_by, created_at, public_id) VALUES
('BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBB1', '11111111-1111-1111-1111-111111111111', 'JE-3001', NOW(), 'Initial Capital Injection', 'VC-01', true, true, '22222222-2222-2222-2222-222222222221', NOW(), 'JE-3001');

-- 13. JOURNAL LINES --
INSERT INTO journal_lines (id, branch_id, journal_entry_id, account_name, entry_type, amount, journal_line_code, account_code) VALUES
('CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCC1', '11111111-1111-1111-1111-111111111111', 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBB1', 'Cash Office', 'debit', 500000000, 'JL-3001-1', 'GL-1001'),
('CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCC2', '11111111-1111-1111-1111-111111111111', 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBB1', 'Equity Capital', 'credit', 500000000, 'JL-3001-2', 'GL-3000');

-- 14. AUDIT LOGS --
INSERT INTO audit_logs (id, branch_id, table_name, record_id, action, before_val, after_val, timestamp, audit_log_code) VALUES
('DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDD1', '11111111-1111-1111-1111-111111111111', 'branches', '11111111-1111-1111-1111-111111111111', 'INSERT', '{}', '{"name": "Initial Setup"}', NOW(), 'AUD0001');

-- 15. DAY ENDS --
INSERT INTO day_ends (id, branch_id, date, is_closed, journals_locked, total_collected, discrepancy, discrepancy_resolved, day_end_code) VALUES
('EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEE1', '11111111-1111-1111-1111-111111111111', NOW() - INTERVAL '1 day', true, 12, 4500000, 0, true, 'DE0001');

-- 16. PROFIT DISTRIBUTIONS --
INSERT INTO profit_distributions (id, branch_id, period, payout_amount, status, processed_at, partner_id, profit_distribution_code) VALUES
('FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFF1', '11111111-1111-1111-1111-111111111111', 'March 2026', 2500000, 'processed', NOW(), '66666666-6666-6666-6666-666666666661', 'PRD0001');

-- 17. CODE SEQUENCES --
INSERT INTO code_sequences (id, entity_name, branch_id, next_sequence_number, code_prefix, created_at, updated_at) VALUES
('E1111111-1111-1111-1111-111111111111', 'Branch', '11111111-1111-1111-1111-111111111111', 2, 'BR', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111112', 'User', '11111111-1111-1111-1111-111111111111', 10, 'USR', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111113', 'Customer', '11111111-1111-1111-1111-111111111111', 3, 'CUS', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111114', 'LoanCase', '11111111-1111-1111-1111-111111111111', 2, 'LN', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111115', 'LoanProduct', '11111111-1111-1111-1111-111111111111', 3, 'PRO', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111116', 'Partner', '11111111-1111-1111-1111-111111111111', 2, 'PAR', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111117', 'CapitalAccount', '11111111-1111-1111-1111-111111111111', 2, 'CAP', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111118', 'Installment', '11111111-1111-1111-1111-111111111111', 3, 'INST', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111119', 'Receipt', '11111111-1111-1111-1111-111111111111', 2, 'RCP', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111120', 'JournalEntry', '11111111-1111-1111-1111-111111111111', 2, 'JE', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111121', 'JournalLine', '11111111-1111-1111-1111-111111111111', 3, 'JL', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111122', 'DayEnd', '11111111-1111-1111-1111-111111111111', 2, 'DE', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111123', 'ProfitDistribution', '11111111-1111-1111-1111-111111111111', 2, 'PFT', NOW(), NOW()),
('E1111111-1111-1111-1111-111111111124', 'CapitalTransaction', '11111111-1111-1111-1111-111111111111', 2, 'CAPTX', NOW(), NOW());
