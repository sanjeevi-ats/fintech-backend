-- Phase 9: Cash Flow Analysis System
-- Created: June 21, 2026

-- Table 1: cash_flow_statement - Cash flow statements
CREATE TABLE cash_flow_statement (
    id UUID PRIMARY KEY,
    period_id UUID REFERENCES accounting_period(id) ON DELETE SET NULL,
    branch_id UUID REFERENCES branch(id) ON DELETE SET NULL,
    operating_cash_flow BIGINT NOT NULL DEFAULT 0,
    investing_cash_flow BIGINT NOT NULL DEFAULT 0,
    financing_cash_flow BIGINT NOT NULL DEFAULT 0,
    net_cash_flow BIGINT NOT NULL DEFAULT 0,
    beginning_balance BIGINT NOT NULL DEFAULT 0,
    ending_balance BIGINT NOT NULL DEFAULT 0,
    statement_date TIMESTAMP NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Draft, 2=Generated, 3=Finalized, 4=Archived
    notes VARCHAR(1000),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_cf_statement_flows CHECK (
        operating_cash_flow >= -9223372036854775807 AND
        investing_cash_flow >= -9223372036854775807 AND
        financing_cash_flow >= -9223372036854775807 AND
        net_cash_flow >= -9223372036854775807
    )
);

-- Table 2: cash_flow_item - Cash flow line items
CREATE TABLE cash_flow_item (
    id UUID PRIMARY KEY,
    cash_flow_statement_id UUID NOT NULL REFERENCES cash_flow_statement(id) ON DELETE CASCADE,
    category INT NOT NULL, -- 1=Operating, 2=Investing, 3=Financing
    item_type INT NOT NULL, -- 1=Inflow, 2=Outflow
    amount BIGINT NOT NULL,
    description VARCHAR(500),
    reference_code VARCHAR(100),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_cf_item_amount CHECK (amount >= 0)
);

-- Table 3: cash_flow_forecast - Cash flow projections
CREATE TABLE cash_flow_forecast (
    id UUID PRIMARY KEY,
    period_id UUID REFERENCES accounting_period(id) ON DELETE SET NULL,
    branch_id UUID REFERENCES branch(id) ON DELETE SET NULL,
    forecast_period DATE NOT NULL,
    projected_cash_flow BIGINT NOT NULL,
    confidence_level INT NOT NULL DEFAULT 5, -- 1-10 scale
    assumptions VARCHAR(1000),
    status INT NOT NULL DEFAULT 1, -- 1=Draft, 2=Generated, 3=Approved
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT chk_cf_forecast_confidence CHECK (confidence_level >= 1 AND confidence_level <= 10)
);

-- Indexes for performance
CREATE INDEX ix_cf_statement_period_id ON cash_flow_statement(period_id);
CREATE INDEX ix_cf_statement_branch_id ON cash_flow_statement(branch_id);
CREATE INDEX ix_cf_statement_statement_date ON cash_flow_statement(statement_date);
CREATE INDEX ix_cf_statement_status ON cash_flow_statement(status);
CREATE INDEX ix_cf_statement_period_branch ON cash_flow_statement(period_id, branch_id);

CREATE INDEX ix_cf_item_statement_id ON cash_flow_item(cash_flow_statement_id);
CREATE INDEX ix_cf_item_category ON cash_flow_item(category);
CREATE INDEX ix_cf_item_type ON cash_flow_item(item_type);
CREATE INDEX ix_cf_item_cf_category ON cash_flow_item(cash_flow_statement_id, category);

CREATE INDEX ix_cf_forecast_period_id ON cash_flow_forecast(period_id);
CREATE INDEX ix_cf_forecast_branch_id ON cash_flow_forecast(branch_id);
CREATE INDEX ix_cf_forecast_forecast_period ON cash_flow_forecast(forecast_period);
CREATE INDEX ix_cf_forecast_status ON cash_flow_forecast(status);

-- Grant permissions (if using database roles)
-- GRANT SELECT, INSERT, UPDATE ON cash_flow_statement TO app_user;
-- GRANT SELECT, INSERT, UPDATE ON cash_flow_item TO app_user;
-- GRANT SELECT, INSERT, UPDATE ON cash_flow_forecast TO app_user;

