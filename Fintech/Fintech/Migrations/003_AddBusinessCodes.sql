-- ============================================================================
-- PHASE 1: ID REFACTORING IMPLEMENTATION - DATABASE SCHEMA
-- Business Code Fields Migration Script
-- ============================================================================
-- This script adds business code support to all 15 core entities.
-- It creates the code_sequences table for tracking sequential code generation
-- and adds code columns to all existing tables.
-- ============================================================================
-- Date: June 13, 2026
-- Version: 1.0
-- Status: Phase 1 - Database Schema
-- ============================================================================

BEGIN TRANSACTION;

-- ============================================================================
-- STEP 1: Create CodeSequence table for tracking business code sequences
-- ============================================================================
CREATE TABLE IF NOT EXISTS code_sequences (
    id UUID PRIMARY KEY,
    entity_name VARCHAR(100) NOT NULL,
    branch_id UUID NOT NULL,
    next_sequence_number INTEGER NOT NULL DEFAULT 1,
    code_prefix VARCHAR(10) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    UNIQUE(entity_name, branch_id)
);

CREATE INDEX IF NOT EXISTS idx_code_sequences_entity_branch 
    ON code_sequences(entity_name, branch_id);

-- ============================================================================
-- STEP 2: Add code columns to all entities
-- ============================================================================

-- Customer codes
ALTER TABLE customers 
ADD COLUMN IF NOT EXISTS customer_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_customers_customer_code 
    ON customers(customer_code) 
    WHERE customer_code IS NOT NULL;

-- LoanCase codes (may already exist)
ALTER TABLE loan_cases 
ADD COLUMN IF NOT EXISTS loan_code_new VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_loan_cases_loan_code_new 
    ON loan_cases(loan_code_new) 
    WHERE loan_code_new IS NOT NULL;

-- LoanProduct codes (may already exist)
ALTER TABLE loan_products 
ADD COLUMN IF NOT EXISTS product_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_loan_products_product_code 
    ON loan_products(product_code) 
    WHERE product_code IS NOT NULL;

-- Installment codes
ALTER TABLE installments 
ADD COLUMN IF NOT EXISTS installment_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_installments_installment_code 
    ON installments(installment_code) 
    WHERE installment_code IS NOT NULL;

-- Receipt codes (may use existing PublicId field, adding new standardized column)
ALTER TABLE receipts 
ADD COLUMN IF NOT EXISTS receipt_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_receipts_receipt_code 
    ON receipts(receipt_code) 
    WHERE receipt_code IS NOT NULL;

-- Partner codes
ALTER TABLE partners 
ADD COLUMN IF NOT EXISTS partner_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_partners_partner_code 
    ON partners(partner_code) 
    WHERE partner_code IS NOT NULL;

-- CapitalAccount codes
ALTER TABLE capital_accounts 
ADD COLUMN IF NOT EXISTS capital_account_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_capital_accounts_capital_account_code 
    ON capital_accounts(capital_account_code) 
    WHERE capital_account_code IS NOT NULL;

-- JournalEntry codes (may use existing PublicId field, adding new standardized column)
ALTER TABLE journal_entries 
ADD COLUMN IF NOT EXISTS journal_entry_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_journal_entries_journal_entry_code 
    ON journal_entries(journal_entry_code) 
    WHERE journal_entry_code IS NOT NULL;

-- JournalLine codes
ALTER TABLE journal_lines 
ADD COLUMN IF NOT EXISTS journal_line_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_journal_lines_journal_line_code 
    ON journal_lines(journal_line_code) 
    WHERE journal_line_code IS NOT NULL;

-- Account codes
ALTER TABLE accounts 
ADD COLUMN IF NOT EXISTS account_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_accounts_account_code 
    ON accounts(account_code) 
    WHERE account_code IS NOT NULL;

-- DayEnd codes
ALTER TABLE day_ends 
ADD COLUMN IF NOT EXISTS day_end_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_day_ends_day_end_code 
    ON day_ends(day_end_code) 
    WHERE day_end_code IS NOT NULL;

-- ProfitDistribution codes
ALTER TABLE profit_distributions 
ADD COLUMN IF NOT EXISTS profit_distribution_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_profit_distributions_profit_distribution_code 
    ON profit_distributions(profit_distribution_code) 
    WHERE profit_distribution_code IS NOT NULL;

-- Branch codes
ALTER TABLE branches 
ADD COLUMN IF NOT EXISTS branch_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_branches_branch_code 
    ON branches(branch_code) 
    WHERE branch_code IS NOT NULL;

-- User codes
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS user_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_users_user_code 
    ON users(user_code) 
    WHERE user_code IS NOT NULL;

-- AuditLog codes
ALTER TABLE audit_logs 
ADD COLUMN IF NOT EXISTS audit_log_code VARCHAR(20) UNIQUE;

CREATE INDEX IF NOT EXISTS idx_audit_logs_audit_log_code 
    ON audit_logs(audit_log_code) 
    WHERE audit_log_code IS NOT NULL;

-- ============================================================================
-- STEP 3: Initialize code sequences for existing branches
-- ============================================================================
-- For each branch that exists, initialize sequences for all entity types
-- This ensures code generation is ready for future records

INSERT INTO code_sequences 
    (id, entity_name, branch_id, next_sequence_number, code_prefix, created_at, updated_at)
SELECT 
    gen_random_uuid() as id,
    entity_name,
    branch_id,
    1 as next_sequence_number,
    code_prefix,
    NOW() as created_at,
    NOW() as updated_at
FROM (
    SELECT DISTINCT 
        'Customer' as entity_name,
        b.id as branch_id,
        'CUS' as code_prefix
    FROM branches b
    UNION ALL
    SELECT 'LoanCase', b.id, 'LN' FROM branches b
    UNION ALL
    SELECT 'LoanProduct', b.id, 'PRO' FROM branches b
    UNION ALL
    SELECT 'Installment', b.id, 'INST' FROM branches b
    UNION ALL
    SELECT 'Receipt', b.id, 'RCP' FROM branches b
    UNION ALL
    SELECT 'Partner', b.id, 'PAR' FROM branches b
    UNION ALL
    SELECT 'CapitalAccount', b.id, 'CAP' FROM branches b
    UNION ALL
    SELECT 'JournalEntry', b.id, 'JE' FROM branches b
    UNION ALL
    SELECT 'JournalLine', b.id, 'JL' FROM branches b
    UNION ALL
    SELECT 'Account', b.id, 'ACC' FROM branches b
    UNION ALL
    SELECT 'DayEnd', b.id, 'DE' FROM branches b
    UNION ALL
    SELECT 'ProfitDistribution', b.id, 'PFT' FROM branches b
    UNION ALL
    SELECT 'Branch', b.id, 'BR' FROM branches b
    UNION ALL
    SELECT 'User', b.id, 'USR' FROM branches b
    UNION ALL
    SELECT 'AuditLog', b.id, 'AUD' FROM branches b
) entities
ON CONFLICT (entity_name, branch_id) DO NOTHING;

-- ============================================================================
-- STEP 4: Optional - Migrate existing data to codes
-- ============================================================================
-- This step generates codes for existing records.
-- Uncomment and customize based on your business requirements.

-- Example: Generate codes for existing customers
-- DO $$
-- DECLARE
--     v_sequence_number INT := 1;
--     v_branch_id UUID;
--     v_customer_id UUID;
-- BEGIN
--     FOR v_branch_id IN SELECT DISTINCT branch_id FROM customers ORDER BY branch_id LOOP
--         v_sequence_number := 1;
--         FOR v_customer_id IN SELECT id FROM customers WHERE branch_id = v_branch_id AND customer_code IS NULL ORDER BY created_at LOOP
--             UPDATE customers 
--             SET customer_code = 'CUS' || LPAD(v_sequence_number::TEXT, 4, '0')
--             WHERE id = v_customer_id;
--             v_sequence_number := v_sequence_number + 1;
--         END LOOP;
--     END LOOP;
-- END $$;

COMMIT TRANSACTION;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================
-- Run these queries to verify the migration succeeded:

-- Check code_sequences table
-- SELECT * FROM code_sequences ORDER BY entity_name, branch_id;

-- Check customer_code column
-- SELECT COUNT(*) as total_customers, COUNT(customer_code) as with_codes FROM customers;

-- Check all code columns
-- SELECT 
--     'customer_code' as code_type,
--     COUNT(*) as total_records,
--     COUNT(customer_code) as with_codes
-- FROM customers
-- UNION ALL
-- SELECT 'loan_code_new', COUNT(*), COUNT(loan_code_new) FROM loan_cases
-- UNION ALL
-- SELECT 'installment_code', COUNT(*), COUNT(installment_code) FROM installments
-- ... etc

-- ============================================================================
-- ROLLBACK PROCEDURE (if needed)
-- ============================================================================
-- If you need to rollback this migration, execute:
-- DROP TABLE IF EXISTS code_sequences;
-- ALTER TABLE customers DROP COLUMN IF EXISTS customer_code;
-- ALTER TABLE loan_cases DROP COLUMN IF EXISTS loan_code_new;
-- ... (repeat for all tables)

-- ============================================================================
-- NEXT STEPS
-- ============================================================================
-- Phase 2: Entity Models - Update C# entity classes (already done)
-- Phase 3: Service Layer - Create CodeGenerationService (already done)
-- Phase 4: Business Services - Update services to generate codes on create
-- Phase 5: DTOs - Update DTOs to include code fields
-- Phase 6: API Controllers - Return codes in responses
-- Phase 7: Frontend - Display codes instead of GUIDs
-- Phase 8: Search - Implement code-based search
-- Phase 9: Reports - Update reports to show codes
-- Phase 10: Testing & Deployment
