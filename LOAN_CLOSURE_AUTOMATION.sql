-- ============================================================================
-- LOAN CLOSURE AUTOMATION PROCEDURE
-- ============================================================================
-- Purpose: Automatically close loans when all installments are paid
-- Trigger: Called after each collection/payment recording
-- Logic: Check if total paid amount >= total receivable, then close loan
-- ============================================================================

DROP PROCEDURE IF EXISTS proc_check_and_close_loan(uuid);

CREATE OR REPLACE PROCEDURE proc_check_and_close_loan(p_loan_id uuid)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch_id UUID;
    v_total_receivable BIGINT;
    v_total_paid BIGINT;
    v_pending_count INT;
    v_je_id UUID;
    v_current_status TEXT;
BEGIN
    -- Get loan details
    SELECT branch_id, total_receivable, status 
    INTO v_branch_id, v_total_receivable, v_current_status
    FROM public.loan_cases 
    WHERE id = p_loan_id;
    
    -- Exit if loan not found or already closed
    IF v_branch_id IS NULL OR v_current_status = 'closed' THEN
        RETURN;
    END IF;
    
    -- Count pending installments
    SELECT COUNT(*) INTO v_pending_count
    FROM public.installments
    WHERE loan_case_id = p_loan_id AND status != 'paid';
    
    -- If no pending installments, close the loan
    IF v_pending_count = 0 THEN
        -- Create journal entry for loan closure
        v_je_id := gen_random_uuid();
        
        INSERT INTO public.journal_entries 
        (id, branch_id, public_id, entry_date, description, reference, is_manual, is_posted)
        VALUES 
        (v_je_id, v_branch_id, 'JE-' || SUBSTRING(v_je_id::text, 1, 8), 
         NOW(), 'Loan Closure - All Installments Paid', p_loan_id::text, false, true);
        
        -- Create journal lines for closure
        INSERT INTO public.journal_lines 
        (id, branch_id, journal_entry_id, account_name, entry_type, amount)
        VALUES
        (gen_random_uuid(), v_branch_id, v_je_id, 'Loan Receivables', 'debit', v_total_receivable),
        (gen_random_uuid(), v_branch_id, v_je_id, 'Loan Portfolio', 'credit', v_total_receivable);
        
        -- Update loan status to closed
        UPDATE public.loan_cases 
        SET status = 'closed' 
        WHERE id = p_loan_id;
        
        -- Log the closure
        INSERT INTO public.audit_logs 
        (id, branch_id, table_name, record_id, action, before_val, after_val, timestamp)
        VALUES
        (gen_random_uuid(), v_branch_id, 'loan_cases', p_loan_id::text, 'UPDATE',
         '{"status":"' || v_current_status || '"}',
         '{"status":"closed"}',
         NOW());
    END IF;
END;
$$;

-- ============================================================================
-- TRIGGER: Automatically check loan closure after payment
-- ============================================================================
DROP TRIGGER IF EXISTS trg_check_loan_closure_after_payment ON public.receipts;

CREATE TRIGGER trg_check_loan_closure_after_payment
AFTER INSERT ON public.receipts
FOR EACH ROW
EXECUTE FUNCTION proc_trigger_check_loan_closure();

-- ============================================================================
-- TRIGGER FUNCTION: Wrapper to call closure check
-- ============================================================================
DROP FUNCTION IF EXISTS proc_trigger_check_loan_closure();

CREATE OR REPLACE FUNCTION proc_trigger_check_loan_closure()
RETURNS TRIGGER AS $$
BEGIN
    CALL proc_check_and_close_loan(NEW.loan_case_id);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- MANUAL CLOSURE PROCEDURE (for NPA or other scenarios)
-- ============================================================================
DROP PROCEDURE IF EXISTS proc_close_loan_manual(uuid, text);

CREATE OR REPLACE PROCEDURE proc_close_loan_manual(p_loan_id uuid, p_reason text)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch_id UUID;
    v_total_receivable BIGINT;
    v_je_id UUID;
BEGIN
    -- Get loan details
    SELECT branch_id, total_receivable 
    INTO v_branch_id, v_total_receivable
    FROM public.loan_cases 
    WHERE id = p_loan_id;
    
    IF v_branch_id IS NULL THEN
        RAISE EXCEPTION 'Loan not found: %', p_loan_id;
    END IF;
    
    -- Create journal entry for manual closure
    v_je_id := gen_random_uuid();
    
    INSERT INTO public.journal_entries 
    (id, branch_id, public_id, entry_date, description, reference, is_manual, is_posted)
    VALUES 
    (v_je_id, v_branch_id, 'JE-' || SUBSTRING(v_je_id::text, 1, 8), 
     NOW(), 'Manual Loan Closure - ' || p_reason, p_loan_id::text, true, true);
    
    -- Create journal lines
    INSERT INTO public.journal_lines 
    (id, branch_id, journal_entry_id, account_name, entry_type, amount)
    VALUES
    (gen_random_uuid(), v_branch_id, v_je_id, 'Loan Receivables', 'debit', v_total_receivable),
    (gen_random_uuid(), v_branch_id, v_je_id, 'Loan Portfolio', 'credit', v_total_receivable);
    
    -- Update loan status
    UPDATE public.loan_cases 
    SET status = 'closed' 
    WHERE id = p_loan_id;
END;
$$;

-- ============================================================================
-- VERIFICATION PROCEDURE: Check loan closure status
-- ============================================================================
DROP PROCEDURE IF EXISTS proc_verify_loan_closure_status(uuid);

CREATE OR REPLACE PROCEDURE proc_verify_loan_closure_status(p_loan_id uuid)
LANGUAGE plpgsql
AS $$
DECLARE
    v_total_installments INT;
    v_paid_installments INT;
    v_pending_installments INT;
    v_total_receivable BIGINT;
    v_total_paid BIGINT;
    v_status TEXT;
BEGIN
    -- Get loan status
    SELECT status INTO v_status FROM public.loan_cases WHERE id = p_loan_id;
    
    -- Count installments
    SELECT COUNT(*) INTO v_total_installments FROM public.installments WHERE loan_case_id = p_loan_id;
    SELECT COUNT(*) INTO v_paid_installments FROM public.installments WHERE loan_case_id = p_loan_id AND status = 'paid';
    SELECT COUNT(*) INTO v_pending_installments FROM public.installments WHERE loan_case_id = p_loan_id AND status != 'paid';
    
    -- Calculate amounts
    SELECT total_receivable INTO v_total_receivable FROM public.loan_cases WHERE id = p_loan_id;
    SELECT COALESCE(SUM(amount_paid), 0) INTO v_total_paid FROM public.receipts WHERE loan_case_id = p_loan_id;
    
    -- Log results
    RAISE NOTICE 'Loan Status: %', v_status;
    RAISE NOTICE 'Total Installments: %, Paid: %, Pending: %', v_total_installments, v_paid_installments, v_pending_installments;
    RAISE NOTICE 'Total Receivable: %, Total Paid: %', v_total_receivable, v_total_paid;
    RAISE NOTICE 'Closure Eligible: %', (v_pending_installments = 0);
END;
$$;

-- ============================================================================
-- EXAMPLE USAGE
-- ============================================================================
-- -- Check and close a specific loan
-- CALL proc_check_and_close_loan('88888888-8888-8888-8888-888888888881');
--
-- -- Manually close a loan (e.g., NPA)
-- CALL proc_close_loan_manual('88888888-8888-8888-8888-888888888881', 'NPA - No payment for 180 days');
--
-- -- Verify closure status
-- CALL proc_verify_loan_closure_status('88888888-8888-8888-8888-888888888881');
