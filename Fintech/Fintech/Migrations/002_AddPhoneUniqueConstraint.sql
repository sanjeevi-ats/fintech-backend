-- Migration: Add unique constraint on customer phone
-- Date: 2026-06-08
-- Purpose: Prevent duplicate customer creation by phone number

-- 1. Add unique constraint on phone (branch-scoped)
ALTER TABLE customers
ADD CONSTRAINT unique_phone_per_branch UNIQUE (phone, branch_id);

-- 2. Create index for efficient lookups
CREATE INDEX IF NOT EXISTS idx_customer_phone ON customers(phone);
CREATE INDEX IF NOT EXISTS idx_customer_phone_branch ON customers(phone, branch_id);

COMMIT;
