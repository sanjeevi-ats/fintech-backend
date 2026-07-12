-- Migration: 012_AddLoanWorkflowFields.sql
-- Purpose: Add submitted_by_id, submitted_at, approved_by_id, approved_at, and rejection_reason columns to loan_cases

ALTER TABLE loan_cases ADD COLUMN IF NOT EXISTS submitted_by_id UUID;
ALTER TABLE loan_cases ADD COLUMN IF NOT EXISTS submitted_at TIMESTAMPTZ;
ALTER TABLE loan_cases ADD COLUMN IF NOT EXISTS approved_by_id UUID;
ALTER TABLE loan_cases ADD COLUMN IF NOT EXISTS approved_at TIMESTAMPTZ;
ALTER TABLE loan_cases ADD COLUMN IF NOT EXISTS rejection_reason TEXT;
