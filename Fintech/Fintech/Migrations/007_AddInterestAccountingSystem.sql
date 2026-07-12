-- Phase 7: Interest Accounting System
-- Created: June 21, 2026

-- Table 1: interest_calculation - Daily interest calculations
CREATE TABLE interest_calculation (
    id UUID PRIMARY KEY,
    loan_id UUID NOT NULL REFERENCES loan_case(id) ON DELETE RESTRICT,
    period_id UUID REFERENCES accounting_period(id) ON DELETE SET NULL,
    daily_rate NUMERIC(10,6) NOT NULL,
    accrual_amount BIGINT NOT NULL,
    calculation_date DATE NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Calculated, 2=Posted, 3=Waived
    interest_type INT NOT NULL DEFAULT 1, -- 1=Fixed, 2=Declining, 3=Variable, 4=StepUp
    description VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_interest_calculation_amount CHECK (accrual_amount >= 0),
    CONSTRAINT chk_interest_calculation_rate CHECK (daily_rate >= 0)
);

-- Table 2: interest_posting - Monthly interest postings
CREATE TABLE interest_posting (
    id UUID PRIMARY KEY,
    loan_id UUID NOT NULL REFERENCES loan_case(id) ON DELETE RESTRICT,
    period_id UUID REFERENCES accounting_period(id) ON DELETE SET NULL,
    posted_amount BIGINT NOT NULL,
    posted_date TIMESTAMP NOT NULL,
    journal_entry_id UUID NOT NULL REFERENCES journal_entry(id) ON DELETE RESTRICT,
    status INT NOT NULL DEFAULT 1, -- 1=Posted, 2=Reversed
    description VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_interest_posting_amount CHECK (posted_amount >= 0)
);

-- Table 3: interest_waiver - Interest waivers and concessions
CREATE TABLE interest_waiver (
    id UUID PRIMARY KEY,
    loan_id UUID NOT NULL REFERENCES loan_case(id) ON DELETE RESTRICT,
    waiver_amount BIGINT NOT NULL,
    waiver_date TIMESTAMP NOT NULL,
    reason VARCHAR(500),
    approved_by VARCHAR(100),
    approval_date TIMESTAMP,
    status INT NOT NULL DEFAULT 1, -- 1=Pending, 2=Approved, 3=Rejected, 4=Reversed
    reversal_journal_entry_id UUID REFERENCES journal_entry(id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_interest_waiver_amount CHECK (waiver_amount >= 0)
);

-- Indexes for performance
CREATE INDEX ix_interest_calculation_loan_id ON interest_calculation(loan_id);
CREATE INDEX ix_interest_calculation_period_id ON interest_calculation(period_id);
CREATE INDEX ix_interest_calculation_calculation_date ON interest_calculation(calculation_date);
CREATE INDEX ix_interest_calculation_status ON interest_calculation(status);
CREATE INDEX ix_interest_calculation_loan_date ON interest_calculation(loan_id, calculation_date);

CREATE INDEX ix_interest_posting_loan_id ON interest_posting(loan_id);
CREATE INDEX ix_interest_posting_period_id ON interest_posting(period_id);
CREATE INDEX ix_interest_posting_posted_date ON interest_posting(posted_date);
CREATE INDEX ix_interest_posting_status ON interest_posting(status);
CREATE INDEX ix_interest_posting_loan_date ON interest_posting(loan_id, posted_date);

CREATE INDEX ix_interest_waiver_loan_id ON interest_waiver(loan_id);
CREATE INDEX ix_interest_waiver_waiver_date ON interest_waiver(waiver_date);
CREATE INDEX ix_interest_waiver_status ON interest_waiver(status);
CREATE INDEX ix_interest_waiver_loan_date ON interest_waiver(loan_id, waiver_date);

-- Grant permissions (if using database roles)
-- GRANT SELECT, INSERT, UPDATE ON interest_calculation TO app_user;
-- GRANT SELECT, INSERT, UPDATE ON interest_posting TO app_user;
-- GRANT SELECT, INSERT, UPDATE ON interest_waiver TO app_user;
