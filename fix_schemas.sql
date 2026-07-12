-- Clean up relationships to change id properties

-- Customers
ALTER TABLE loan_cases DROP CONSTRAINT IF EXISTS fk_loan_cases_customers_customer_id;
ALTER TABLE customers ALTER COLUMN id TYPE uuid USING id::uuid;

-- Loan Cases
ALTER TABLE installments DROP CONSTRAINT IF EXISTS fk_installments_loan_cases_loan_case_id;
ALTER TABLE receipts DROP CONSTRAINT IF EXISTS fk_receipts_loan_cases_loan_case_id;
ALTER TABLE loan_cases ALTER COLUMN id TYPE uuid USING id::uuid;
ALTER TABLE loan_cases ALTER COLUMN customer_id TYPE uuid USING customer_id::uuid;

-- Re-apply to Loan Cases
ALTER TABLE loan_cases ADD CONSTRAINT fk_loan_cases_customers_customer_id FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;

-- Installments
ALTER TABLE receipts DROP CONSTRAINT IF EXISTS fk_receipts_installments_installment_id; -- Just in case
ALTER TABLE installments ALTER COLUMN id TYPE uuid USING id::uuid;
ALTER TABLE installments ALTER COLUMN loan_case_id TYPE uuid USING loan_case_id::uuid;

-- Re-apply to Installments
ALTER TABLE installments ADD CONSTRAINT fk_installments_loan_cases_loan_case_id FOREIGN KEY (loan_case_id) REFERENCES loan_cases(id) ON DELETE CASCADE;

-- Receipts
ALTER TABLE receipts ALTER COLUMN id TYPE uuid USING id::uuid;
ALTER TABLE receipts ALTER COLUMN loan_case_id TYPE uuid USING loan_case_id::uuid;
ALTER TABLE receipts ALTER COLUMN installment_id TYPE uuid USING installment_id::uuid;

-- Re-apply to Receipts
ALTER TABLE receipts ADD CONSTRAINT fk_receipts_loan_cases_loan_case_id FOREIGN KEY (loan_case_id) REFERENCES loan_cases(id) ON DELETE CASCADE;

-- Journal Entries
ALTER TABLE journal_lines DROP CONSTRAINT IF EXISTS fk_journal_lines_journal_entries_journal_entry_id;
ALTER TABLE journal_entries ALTER COLUMN id TYPE uuid USING id::uuid;

-- Journal Lines
ALTER TABLE journal_lines ALTER COLUMN id TYPE uuid USING id::uuid;
ALTER TABLE journal_lines ALTER COLUMN journal_entry_id TYPE uuid USING journal_entry_id::uuid;

-- Re-apply to Journal Lines
ALTER TABLE journal_lines ADD CONSTRAINT fk_journal_lines_journal_entries_journal_entry_id FOREIGN KEY (journal_entry_id) REFERENCES journal_entries(id) ON DELETE CASCADE;

-- Add Missing Columns
ALTER TABLE partners ADD COLUMN IF NOT EXISTS email TEXT DEFAULT '';
ALTER TABLE partners ADD COLUMN IF NOT EXISTS phone TEXT DEFAULT '';

ALTER TABLE loan_products ADD COLUMN IF NOT EXISTS code TEXT DEFAULT '';
ALTER TABLE loan_products ADD COLUMN IF NOT EXISTS default_tenure_months INT DEFAULT 12;

-- Rename Interest Rate
ALTER TABLE loan_products RENAME COLUMN interest_rate_pct TO interest_rate;
