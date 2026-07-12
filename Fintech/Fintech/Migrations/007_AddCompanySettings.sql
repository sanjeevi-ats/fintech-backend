-- =====================================================
-- Migration: Add Company Settings Table
-- Description: Stores company and branch configuration
-- Date: July 12, 2026
-- =====================================================

-- Create CompanySettings table
CREATE TABLE IF NOT EXISTS "CompanySettings" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(200) NOT NULL,
    "FullName" VARCHAR(500) NOT NULL,
    "Address" VARCHAR(500) NOT NULL,
    "City" VARCHAR(100) NOT NULL,
    "State" VARCHAR(100) NOT NULL,
    "Country" VARCHAR(100) NOT NULL,
    "PinCode" VARCHAR(20) NOT NULL,
    "Phone" VARCHAR(50) NOT NULL,
    "Email" VARCHAR(200) NOT NULL,
    "Website" VARCHAR(200),
    "GstNumber" VARCHAR(50),
    "LicenseNumber" VARCHAR(100),
    "RegistrationNumber" VARCHAR(100),
    "Logo" TEXT,
    "Tagline" VARCHAR(500),
    "BusinessType" VARCHAR(100),
    "ReceiptPrefix" VARCHAR(20) DEFAULT 'RCP',
    "ReceiptFooterText" TEXT,
    "ShowBranchDetails" BOOLEAN DEFAULT true,
    "ShowTerminalDetails" BOOLEAN DEFAULT true,
    "AutoEmailReceipts" BOOLEAN DEFAULT false,
    "ReceiptLanguage" VARCHAR(10) DEFAULT 'en',
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create BranchSettings table (extended from existing Branches)
CREATE TABLE IF NOT EXISTS "BranchSettings" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "BranchId" UUID NOT NULL REFERENCES "Branches"("Id") ON DELETE CASCADE,
    "TerminalPrefix" VARCHAR(20) DEFAULT 'POS',
    "PrinterName" VARCHAR(200),
    "CashDrawerEnabled" BOOLEAN DEFAULT false,
    "ReceiptCopies" INT DEFAULT 1,
    "AutoPrintReceipts" BOOLEAN DEFAULT false,
    "ShowQRCode" BOOLEAN DEFAULT true,
    "CustomFooter" TEXT,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("BranchId")
);

-- Insert default company settings for Vettri Finance
INSERT INTO "CompanySettings" (
    "Id",
    "Name",
    "FullName",
    "Address",
    "City",
    "State",
    "Country",
    "PinCode",
    "Phone",
    "Email",
    "Website",
    "GstNumber",
    "LicenseNumber",
    "RegistrationNumber",
    "Logo",
    "Tagline",
    "BusinessType",
    "ReceiptPrefix",
    "ReceiptFooterText",
    "ShowBranchDetails",
    "ShowTerminalDetails",
    "AutoEmailReceipts",
    "ReceiptLanguage"
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Vettri Finance',
    'Vettri Finance Pvt Ltd',
    'Main Road, Financial District',
    'Thiruvannamalai',
    'Tamil Nadu',
    'India',
    '606601',
    '+91 4175 234567',
    'support@vettrifinance.com',
    'www.vettrifinance.com',
    '33AABCV9603R1ZM',
    'NBFC-MFI-TN-001/2024',
    'U65993TN2024PTC123456',
    '/assets/logo.png',
    'Empowering Financial Dreams',
    'Microfinance Institution',
    'RCP',
    'Thank you for choosing Vettri Finance. For any queries, please contact our customer support.',
    true,
    true,
    false,
    'en'
) ON CONFLICT ("Id") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "FullName" = EXCLUDED."FullName",
    "Address" = EXCLUDED."Address",
    "City" = EXCLUDED."City",
    "State" = EXCLUDED."State",
    "UpdatedAt" = CURRENT_TIMESTAMP;

-- Create indexes
CREATE INDEX IF NOT EXISTS "idx_company_settings_active" ON "CompanySettings"("Id");
CREATE INDEX IF NOT EXISTS "idx_branch_settings_branch" ON "BranchSettings"("BranchId");

-- Add update trigger
CREATE OR REPLACE FUNCTION update_company_settings_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_company_settings_timestamp
BEFORE UPDATE ON "CompanySettings"
FOR EACH ROW
EXECUTE FUNCTION update_company_settings_timestamp();

CREATE TRIGGER trigger_update_branch_settings_timestamp
BEFORE UPDATE ON "BranchSettings"
FOR EACH ROW
EXECUTE FUNCTION update_company_settings_timestamp();

-- Verify installation
SELECT 
    'CompanySettings table created' as status,
    COUNT(*) as record_count 
FROM "CompanySettings";

SELECT 
    'BranchSettings table created' as status,
    COUNT(*) as record_count 
FROM "BranchSettings";
