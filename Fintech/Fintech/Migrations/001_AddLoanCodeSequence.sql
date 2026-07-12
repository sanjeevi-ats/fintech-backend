-- Migration: Add Loan Code (LN00001, LN00002) support
-- Date: 2026-06-08
-- Purpose: Add unique 3-digit loan code generation for loan tracking

-- 1. Create sequence for loan code generation
CREATE SEQUENCE IF NOT EXISTS loan_code_seq START WITH 1 INCREMENT BY 1;

-- 2. Add loan_code column to loan_cases if it doesn't exist
ALTER TABLE loan_cases
ADD COLUMN IF NOT EXISTS loan_code VARCHAR(20) UNIQUE;

-- 3. Create index for fast lookups
CREATE INDEX IF NOT EXISTS idx_loan_code ON loan_cases(loan_code);
CREATE INDEX IF NOT EXISTS idx_loan_code_status ON loan_cases(loan_code, status);

-- 4. Populate existing loans with loan codes (one-time operation)
-- NOTE: Run this only once after deployment
-- UPDATE loan_cases 
-- SET loan_code = CONCAT('LN', LPAD(ROW_NUMBER() OVER (ORDER BY created_at), 5, '0'))
-- WHERE loan_code IS NULL;

-- 5. Add not-null constraint after data is populated
-- ALTER TABLE loan_cases 
-- ALTER COLUMN loan_code SET NOT NULL;

COMMIT;
