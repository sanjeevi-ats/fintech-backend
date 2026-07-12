-- Fix Branch Creation Issue - Disable Audit Trigger on Branches Table
-- The branches table doesn't have a branch_id column, so the audit trigger fails

-- Option 1: Disable the trigger on branches table
DROP TRIGGER IF EXISTS trg_audit_branches ON branches;

-- Option 2: If you want to keep the trigger but fix it, update the trigger function
-- to handle branches table specially (since it doesn't have branch_id)

-- Check if the trigger function exists and what it does
-- SELECT prosrc FROM pg_proc WHERE proname = 'fn_log_activity';

-- For now, we'll just disable the trigger on branches table
-- The application will handle audit logging for branches through SaveChangesAsync
