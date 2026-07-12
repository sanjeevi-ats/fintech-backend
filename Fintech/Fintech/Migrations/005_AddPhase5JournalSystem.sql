-- Phase 5: Double-Entry Accounting System Migration
-- Adds support for GL account mappings, ledger balances, and journal entry tracking

-- Step 1: Add columns to journal_entries table for Phase 5 tracking
ALTER TABLE journal_entries
ADD COLUMN capital_transaction_id UUID NULL,
ADD COLUMN reversal_of_entry_id UUID NULL,
ADD COLUMN created_by UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
ADD COLUMN created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;

-- Step 2: Add account_code column to journal_lines table
ALTER TABLE journal_lines
ADD COLUMN account_code VARCHAR(20) NOT NULL DEFAULT 'GL-0000';

-- Step 3: Create AccountMapping table
CREATE TABLE IF NOT EXISTS account_mappings (
    id UUID PRIMARY KEY,
    branch_id UUID NOT NULL,
    capital_account_code VARCHAR(50) NOT NULL,
    capital_account_id UUID NOT NULL,
    gl_account_code VARCHAR(20) NOT NULL,
    gl_account_id UUID NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL,
    CONSTRAINT fk_account_mappings_branch FOREIGN KEY (branch_id) REFERENCES branches(id),
    UNIQUE(branch_id, capital_account_code)
);

-- Step 4: Create LedgerBalance table
CREATE TABLE IF NOT EXISTS ledger_balances (
    id UUID PRIMARY KEY,
    branch_id UUID NOT NULL,
    gl_account_id UUID NOT NULL,
    gl_account_code VARCHAR(20) NOT NULL,
    account_name VARCHAR(255) NOT NULL,
    balance BIGINT NOT NULL DEFAULT 0,
    total_debits BIGINT NOT NULL DEFAULT 0,
    total_credits BIGINT NOT NULL DEFAULT 0,
    version INT NOT NULL DEFAULT 1,
    last_updated TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_ledger_balances_branch FOREIGN KEY (branch_id) REFERENCES branches(id),
    CONSTRAINT fk_ledger_balances_account FOREIGN KEY (gl_account_id) REFERENCES accounts(id),
    UNIQUE(branch_id, gl_account_code)
);

-- Step 5: Create LedgerHistory table
CREATE TABLE IF NOT EXISTS ledger_histories (
    id UUID PRIMARY KEY,
    branch_id UUID NOT NULL,
    gl_account_id UUID NOT NULL,
    gl_account_code VARCHAR(20) NOT NULL,
    date TIMESTAMP NOT NULL,
    balance BIGINT NOT NULL DEFAULT 0,
    debits BIGINT NOT NULL DEFAULT 0,
    credits BIGINT NOT NULL DEFAULT 0,
    recorded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_ledger_histories_branch FOREIGN KEY (branch_id) REFERENCES branches(id),
    CONSTRAINT fk_ledger_histories_account FOREIGN KEY (gl_account_id) REFERENCES accounts(id)
);

-- Step 6: Create indexes for performance

-- JournalEntry indexes
CREATE INDEX IF NOT EXISTS ix_journal_entries_date ON journal_entries(entry_date DESC);
CREATE INDEX IF NOT EXISTS ix_journal_entries_capital_transaction ON journal_entries(capital_transaction_id);
CREATE INDEX IF NOT EXISTS ix_journal_entries_posting ON journal_entries(is_posted, entry_date DESC);

-- JournalLine indexes
CREATE INDEX IF NOT EXISTS ix_journal_lines_entry ON journal_lines(journal_entry_id);
CREATE INDEX IF NOT EXISTS ix_journal_lines_account ON journal_lines(account_code);

-- LedgerBalance indexes
CREATE INDEX IF NOT EXISTS ix_ledger_balances_account ON ledger_balances(gl_account_code);

-- LedgerHistory indexes
CREATE INDEX IF NOT EXISTS ix_ledger_histories_account_date ON ledger_histories(gl_account_code, date);

-- AccountMapping indexes
CREATE INDEX IF NOT EXISTS ix_account_mappings_gl_account ON account_mappings(gl_account_code);

-- Step 7: Seed default GL accounts (only if they don't exist)
INSERT INTO accounts (id, branch_id, name, account_code)
SELECT 
    '11111111-1111-1111-1111-111111111111'::UUID,
    '00000000-0000-0000-0000-000000000001'::UUID,
    'Bank/Asset',
    'GL-1000'
WHERE NOT EXISTS (SELECT 1 FROM accounts WHERE account_code = 'GL-1000')
UNION ALL
SELECT 
    '22222222-2222-2222-2222-222222222222'::UUID,
    '00000000-0000-0000-0000-000000000001'::UUID,
    'Capital/Equity',
    'GL-3000'
WHERE NOT EXISTS (SELECT 1 FROM accounts WHERE account_code = 'GL-3000');

-- Step 8: Ensure CodeSequence entries exist for JournalEntry and JournalLine generation
INSERT INTO code_sequences (id, entity_name, code_prefix, current_value, branch_id)
SELECT 
    '33333333-3333-3333-3333-333333333333'::UUID,
    'JournalEntry',
    'JE',
    0,
    '00000000-0000-0000-0000-000000000001'::UUID
WHERE NOT EXISTS (SELECT 1 FROM code_sequences WHERE entity_name = 'JournalEntry')
UNION ALL
SELECT 
    '44444444-4444-4444-4444-444444444444'::UUID,
    'JournalLine',
    'JL',
    0,
    '00000000-0000-0000-0000-000000000001'::UUID
WHERE NOT EXISTS (SELECT 1 FROM code_sequences WHERE entity_name = 'JournalLine');

-- Step 9: Initialize default account mappings for capital accounts to GL-3000
-- This assumes capital accounts exist from Phase 4
INSERT INTO account_mappings (id, branch_id, capital_account_code, capital_account_id, gl_account_code, gl_account_id, is_active)
SELECT 
    gen_random_uuid(),
    ca.branch_id,
    ca.capital_account_code,
    ca.id,
    'GL-3000',
    '22222222-2222-2222-2222-222222222222'::UUID,
    true
FROM capital_accounts ca
WHERE NOT EXISTS (
    SELECT 1 FROM account_mappings am 
    WHERE am.capital_account_id = ca.id 
    AND am.is_active = true
);

-- Step 10: Message
-- Migration complete - Phase 5 double-entry accounting system initialized
