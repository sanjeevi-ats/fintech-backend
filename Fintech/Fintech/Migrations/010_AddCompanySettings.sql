-- Migration: 010_AddCompanySettings.sql
-- Purpose: Create company_settings table for company info, logo, and tax details
-- Applied: Run this script against the production PostgreSQL database

CREATE TABLE IF NOT EXISTS company_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_name VARCHAR(200) NOT NULL DEFAULT 'ABC Finance Pvt Ltd',
    logo_base64 TEXT,
    logo_mime_type VARCHAR(50),
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(100),
    pincode VARCHAR(10),
    phone VARCHAR(20),
    email VARCHAR(200),
    website VARCHAR(200),
    gst_number VARCHAR(30),
    pan_number VARCHAR(20),
    cin_number VARCHAR(30),
    tagline VARCHAR(300),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Insert default row so GET always returns something
INSERT INTO company_settings (company_name, tagline, updated_at)
VALUES ('FinVeda Microfinance', 'Empowering communities through accessible finance', NOW())
ON CONFLICT DO NOTHING;
