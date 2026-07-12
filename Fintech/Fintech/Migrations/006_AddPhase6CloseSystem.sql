-- Phase 6: Month-End Close System

-- Accounting Period Table
CREATE TABLE IF NOT EXISTS AccountingPeriod (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    PeriodCode VARCHAR(10) NOT NULL UNIQUE,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status INT NOT NULL DEFAULT 1,
    ClosedAt TIMESTAMP NULL,
    ClosedBy VARCHAR(100) NULL,
    BranchId UUID NOT NULL REFERENCES Branch(Id),
    Version INT NOT NULL DEFAULT 1,
    Notes TEXT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NULL,
    IsDeleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS IX_AccountingPeriod_BranchId_Status ON AccountingPeriod(BranchId, Status);
CREATE INDEX IF NOT EXISTS IX_AccountingPeriod_PeriodCode ON AccountingPeriod(PeriodCode);
CREATE UNIQUE INDEX IF NOT EXISTS UK_AccountingPeriod_Code_Branch ON AccountingPeriod(PeriodCode, BranchId) WHERE IsDeleted = FALSE;

-- Accrual Entry Table
CREATE TABLE IF NOT EXISTS AccrualEntry (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    PeriodId UUID NOT NULL REFERENCES AccountingPeriod(Id),
    LoanId UUID NULL REFERENCES LoanCase(Id),
    Type INT NOT NULL,
    Amount BIGINT NOT NULL,
    JournalEntryId UUID NOT NULL REFERENCES JournalEntry(Id),
    ReversalJournalEntryId UUID NULL REFERENCES JournalEntry(Id),
    AccrualDate DATE NOT NULL,
    Description VARCHAR(255),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS IX_AccrualEntry_PeriodId ON AccrualEntry(PeriodId);
CREATE INDEX IF NOT EXISTS IX_AccrualEntry_LoanId ON AccrualEntry(LoanId);
CREATE INDEX IF NOT EXISTS IX_AccrualEntry_Type ON AccrualEntry(Type);

-- Provision Entry Table
CREATE TABLE IF NOT EXISTS ProvisionEntry (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    PeriodId UUID NOT NULL REFERENCES AccountingPeriod(Id),
    LoanId UUID NULL REFERENCES LoanCase(Id),
    Type INT NOT NULL,
    Amount BIGINT NOT NULL,
    PreviousAmount BIGINT NOT NULL DEFAULT 0,
    JournalEntryId UUID NOT NULL REFERENCES JournalEntry(Id),
    Status INT NOT NULL DEFAULT 1,
    CalculationMethod VARCHAR(100),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NULL,
    IsDeleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS IX_ProvisionEntry_PeriodId ON ProvisionEntry(PeriodId);
CREATE INDEX IF NOT EXISTS IX_ProvisionEntry_LoanId ON ProvisionEntry(LoanId);
CREATE INDEX IF NOT EXISTS IX_ProvisionEntry_Status ON ProvisionEntry(Status);

-- Period Reversal Table
CREATE TABLE IF NOT EXISTS PeriodReversal (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    PeriodId UUID NOT NULL REFERENCES AccountingPeriod(Id),
    ReversedBy VARCHAR(100) NOT NULL,
    Reason VARCHAR(500),
    DeletedAccruals INT,
    DeletedProvisions INT,
    DeletedJournalEntries INT,
    ReversedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Initialize accounting periods for 2026
INSERT INTO AccountingPeriod (PeriodCode, StartDate, EndDate, Status, BranchId, Version)
SELECT 
    TO_CHAR(date_series, 'YYYY-MM') AS PeriodCode,
    DATE_TRUNC('month', date_series)::DATE AS StartDate,
    (DATE_TRUNC('month', date_series) + INTERVAL '1 month' - INTERVAL '1 day')::DATE AS EndDate,
    1 AS Status,
    Id AS BranchId,
    1 AS Version
FROM Branch,
    GENERATE_SERIES('2026-01-01'::DATE, '2026-12-01'::DATE, '1 month'::INTERVAL) AS date_series
WHERE IsDeleted = FALSE
AND NOT EXISTS (
    SELECT 1 FROM AccountingPeriod ap 
    WHERE ap.BranchId = Branch.Id 
    AND ap.PeriodCode = TO_CHAR(date_series, 'YYYY-MM')
);

-- Add GL Accounts for accruals and provisions
INSERT INTO GeneralLedger (Code, Name, Type, Balance, Version, BranchId, CreatedAt, IsDeleted)
SELECT 'GL-1500', 'Interest Receivable', 'Asset', 0, 1, Id, CURRENT_TIMESTAMP, FALSE FROM Branch LIMIT 1
ON CONFLICT (Code) DO NOTHING;

INSERT INTO GeneralLedger (Code, Name, Type, Balance, Version, BranchId, CreatedAt, IsDeleted)
SELECT 'GL-4100', 'Interest Income', 'Income', 0, 1, Id, CURRENT_TIMESTAMP, FALSE FROM Branch LIMIT 1
ON CONFLICT (Code) DO NOTHING;

INSERT INTO GeneralLedger (Code, Name, Type, Balance, Version, BranchId, CreatedAt, IsDeleted)
SELECT 'GL-5300', 'Provision Expense', 'Expense', 0, 1, Id, CURRENT_TIMESTAMP, FALSE FROM Branch LIMIT 1
ON CONFLICT (Code) DO NOTHING;

INSERT INTO GeneralLedger (Code, Name, Type, Balance, Version, BranchId, CreatedAt, IsDeleted)
SELECT 'GL-2500', 'Provision Reserve', 'Liability', 0, 1, Id, CURRENT_TIMESTAMP, FALSE FROM Branch LIMIT 1
ON CONFLICT (Code) DO NOTHING;
