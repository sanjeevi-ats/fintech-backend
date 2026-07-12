-- Phase 4: Capital Account Management Migration
-- Date: June 21, 2026
-- Purpose: Create tables for capital account tracking, transactions, and history

-- Add columns to existing CapitalAccount table
ALTER TABLE CapitalAccounts ADD COLUMN
    OpeningBalance BIGINT NOT NULL DEFAULT 0,  -- in Paise
    CurrentBalance BIGINT NOT NULL DEFAULT 0,  -- in Paise
    OwnershipPercentage DECIMAL(10, 4) NOT NULL DEFAULT 0.0000,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
    Currency NVARCHAR(3) NOT NULL DEFAULT 'INR',
    LastModifiedBy UNIQUEIDENTIFIER,
    LastModifiedAt DATETIME2;

-- Create CapitalTransaction table
CREATE TABLE CapitalTransactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TransactionCode NVARCHAR(50) NOT NULL UNIQUE,
    CapitalAccountId UNIQUEIDENTIFIER NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL,
    Amount BIGINT NOT NULL,  -- in Paise
    TransactionDate DATE NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    ReferenceNumber NVARCHAR(100),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    ApprovedBy UNIQUEIDENTIFIER,
    ApprovedAt DATETIME2,
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedBy UNIQUEIDENTIFIER,
    LastModifiedAt DATETIME2,
    CONSTRAINT FK_CapitalTransaction_CapitalAccount 
        FOREIGN KEY (CapitalAccountId) 
        REFERENCES CapitalAccounts(Id) 
        ON DELETE NO ACTION,
    CONSTRAINT FK_CapitalTransaction_ApprovedBy 
        FOREIGN KEY (ApprovedBy) 
        REFERENCES Users(Id) 
        ON DELETE SET NULL,
    CONSTRAINT FK_CapitalTransaction_CreatedBy 
        FOREIGN KEY (CreatedBy) 
        REFERENCES Users(Id) 
        ON DELETE NO ACTION
);

-- Create index on TransactionCode
CREATE UNIQUE INDEX IX_CapitalTransaction_TransactionCode 
    ON CapitalTransactions(TransactionCode);

-- Create index on CapitalAccountId and TransactionDate
CREATE INDEX IX_CapitalTransaction_AccountId_Date 
    ON CapitalTransactions(CapitalAccountId, TransactionDate);

-- Create index on Status for filtering
CREATE INDEX IX_CapitalTransaction_Status 
    ON CapitalTransactions(Status);

-- Create CapitalAccountHistory table for historical tracking
CREATE TABLE CapitalAccountHistories (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CapitalAccountId UNIQUEIDENTIFIER NOT NULL,
    EffectiveDate DATE NOT NULL,
    Balance BIGINT NOT NULL,  -- in Paise
    OwnershipPercentage DECIMAL(10, 4) NOT NULL,
    TotalCapitalAtDate BIGINT NOT NULL,  -- in Paise
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_CapitalAccountHistory_CapitalAccount 
        FOREIGN KEY (CapitalAccountId) 
        REFERENCES CapitalAccounts(Id) 
        ON DELETE NO ACTION
);

-- Create composite unique index on AccountId and EffectiveDate
CREATE UNIQUE INDEX IX_CapitalAccountHistory_Unique 
    ON CapitalAccountHistories(CapitalAccountId, EffectiveDate);

-- Create index for historical queries
CREATE INDEX IX_CapitalAccountHistory_EffectiveDate 
    ON CapitalAccountHistories(EffectiveDate);

-- Add index on CapitalAccountCode if not exists
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CapitalAccount_Code')
CREATE UNIQUE INDEX IX_CapitalAccount_Code 
    ON CapitalAccounts(CapitalAccountCode)
    WHERE CapitalAccountCode IS NOT NULL;

-- Create stored procedure to calculate ownership percentages
CREATE OR ALTER PROCEDURE sp_RecalculateOwnershipPercentages
    @AsOfDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @AsOfDate IS NULL
        SET @AsOfDate = GETUTCDATE();
    
    DECLARE @TotalCapital BIGINT;
    
    -- Calculate total capital
    SELECT @TotalCapital = SUM(CurrentBalance)
    FROM CapitalAccounts
    WHERE Status = 'Active';
    
    -- Update ownership percentages
    UPDATE CapitalAccounts
    SET OwnershipPercentage = CASE 
        WHEN @TotalCapital > 0 THEN (CONVERT(DECIMAL(10, 4), CurrentBalance) / CONVERT(DECIMAL(10, 4), @TotalCapital)) * 100
        ELSE 0
    END
    WHERE Status = 'Active';
    
    -- Insert historical record for today if not exists
    IF NOT EXISTS (
        SELECT 1 FROM CapitalAccountHistories 
        WHERE CAST(EffectiveDate AS DATE) = CAST(@AsOfDate AS DATE)
    )
    BEGIN
        INSERT INTO CapitalAccountHistories (Id, CapitalAccountId, EffectiveDate, Balance, OwnershipPercentage, TotalCapitalAtDate)
        SELECT 
            NEWID(),
            Id,
            CAST(@AsOfDate AS DATE),
            CurrentBalance,
            OwnershipPercentage,
            @TotalCapital
        FROM CapitalAccounts
        WHERE Status = 'Active';
    END
END;

-- Seed data for code sequences (if not exists)
IF NOT EXISTS (SELECT 1 FROM CodeSequences WHERE EntityType = 'CapitalAccount')
    INSERT INTO CodeSequences (Id, EntityType, LastNumber, Prefix, CreatedAt)
    VALUES (NEWID(), 'CapitalAccount', 0, 'CAP', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM CodeSequences WHERE EntityType = 'CapitalTransaction')
    INSERT INTO CodeSequences (Id, EntityType, LastNumber, Prefix, CreatedAt)
    VALUES (NEWID(), 'CapitalTransaction', 0, 'CAPTX', GETUTCDATE());

-- Grant execute permissions on stored procedure
GRANT EXECUTE ON sp_RecalculateOwnershipPercentages TO PUBLIC;
