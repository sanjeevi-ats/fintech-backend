-- Migration: Add CustomerCode column to customers table
-- Date: June 17, 2026
-- Purpose: Support customer business code in API responses

-- Check if column already exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name='customers' AND column_name='customer_code'
    ) THEN
        ALTER TABLE customers ADD COLUMN customer_code VARCHAR(50) NULL;
        RAISE NOTICE 'Added customer_code column to customers table';
    ELSE
        RAISE NOTICE 'customer_code column already exists';
    END IF;
END $$;

-- Populate customer_code for existing records
UPDATE customers SET customer_code = 'CUS0001' WHERE id = '44444444-4444-4444-4444-444444444441';
UPDATE customers SET customer_code = 'CUS0002' WHERE id = '44444444-4444-4444-4444-444444444442';

-- Verify update
SELECT id, name, customer_code FROM customers;
