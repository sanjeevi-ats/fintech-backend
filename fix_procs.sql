DROP PROCEDURE IF EXISTS proc_disburse_loan(text, uuid, uuid);
DROP PROCEDURE IF EXISTS proc_disburse_loan(text);
DROP PROCEDURE IF EXISTS proc_disburse_loan(uuid);
DROP PROCEDURE IF EXISTS proc_approve_loan(text);
DROP PROCEDURE IF EXISTS proc_approve_loan(uuid);

CREATE OR REPLACE PROCEDURE proc_approve_loan(p_loan_id uuid)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.loan_cases SET status = 'pending_disburse' WHERE id = p_loan_id;
END;
$$;

CREATE OR REPLACE PROCEDURE proc_disburse_loan(p_loan_id uuid)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch_id UUID;
    v_amount BIGINT;
    v_je_id UUID := gen_random_uuid();
BEGIN
    SELECT branch_id, finance_amount INTO v_branch_id, v_amount FROM public.loan_cases WHERE id = p_loan_id;
    
    INSERT INTO public.journal_entries (id, branch_id, public_id, entry_date, description, reference, is_manual, is_posted)
    VALUES (v_je_id, v_branch_id, 'PUB-' || v_je_id::text, now(), 'Loan Disbursement', p_loan_id::text, false, true);
    
    INSERT INTO public.journal_lines (id, branch_id, journal_entry_id, account_name, entry_type, amount) VALUES
    (gen_random_uuid(), v_branch_id, v_je_id, 'Loan Receivable', 'debit', v_amount),
    (gen_random_uuid(), v_branch_id, v_je_id, 'Cash in Hand', 'credit', v_amount);
    
    UPDATE public.loan_cases SET status = 'active' WHERE id = p_loan_id;
END;
$$;
