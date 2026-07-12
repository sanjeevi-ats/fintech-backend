-- Phase 8: P&L Statements System
-- Created: June 21, 2026

-- Table 1: profit_loss_statement - P&L statement records
CREATE TABLE profit_loss_statement (
    id UUID PRIMARY KEY,
    period_id UUID REFERENCES accounting_period(id) ON DELETE SET NULL,
    branch_id UUID REFERENCES branch(id) ON DELETE SET NULL,
    total_revenue BIGINT NOT NULL DEFAULT 0,
    total_expenses BIGINT NOT NULL DEFAULT 0,
    gross_profit BIGINT NOT NULL DEFAULT 0,
    net_profit BIGINT NOT NULL DEFAULT 0,
    profit_margin NUMERIC(5,2) NOT NULL DEFAULT 0,
    statement_date TIMESTAMP NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Draft, 2=Generated, 3=Finalized, 4=Archived
    notes VARCHAR(1000),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_pl_revenue CHECK (total_revenue >= 0),
    CONSTRAINT chk_pl_expenses CHECK (total_expenses >= 0),
    CONSTRAINT chk_pl_margin CHECK (profit_margin >= 0 AND profit_margin <= 100)
);

-- Table 2: revenue_line - Revenue line items
CREATE TABLE revenue_line (
    id UUID PRIMARY KEY,
    pl_statement_id UUID NOT NULL REFERENCES profit_loss_statement(id) ON DELETE CASCADE,
    category INT NOT NULL, -- 1=InterestIncome, 2=Fees, 3=Penalties, 4=Other
    amount BIGINT NOT NULL,
    description VARCHAR(500),
    reference_code VARCHAR(100),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_revenue_amount CHECK (amount >= 0)
);

-- Table 3: expense_line - Expense line items
CREATE TABLE expense_line (
    id UUID PRIMARY KEY,
    pl_statement_id UUID NOT NULL REFERENCES profit_loss_statement(id) ON DELETE CASCADE,
    category INT NOT NULL, -- 1=Provisions, 2=Waivers, 3=Operating, 4=Administrative
    amount BIGINT NOT NULL,
    description VARCHAR(500),
    reference_code VARCHAR(100),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_expense_amount CHECK (amount >= 0)
);

-- Indexes for performance
CREATE INDEX ix_pl_statement_period_id ON profit_loss_statement(period_id);
CREATE INDEX ix_pl_statement_branch_id ON profit_loss_statement(branch_id);
CREATE INDEX ix_pl_statement_statement_date ON profit_loss_statement(statement_date);
CREATE INDEX ix_pl_statement_status ON profit_loss_statement(status);
CREATE INDEX ix_pl_statement_period_branch ON profit_loss_statement(period_id, branch_id);

CREATE INDEX ix_revenue_line_pl_statement_id ON revenue_line(pl_statement_id);
CREATE INDEX ix_revenue_line_category ON revenue_line(category);
CREATE INDEX ix_revenue_line_pl_category ON revenue_line(pl_statement_id, category);

CREATE INDEX ix_expense_line_pl_statement_id ON expense_line(pl_statement_id);
CREATE INDEX ix_expense_line_category ON expense_line(category);
CREATE INDEX ix_expense_line_pl_category ON expense_line(pl_statement_id, category);

-- Grant permissions (if using database roles)
-- GRANT SELECT, INSERT, UPDATE ON profit_loss_statement TO app_user;
-- GRANT SELECT, INSERT, UPDATE ON revenue_line TO app_user;
-- GRANT SELECT, INSERT, UPDATE ON expense_line TO app_user;

