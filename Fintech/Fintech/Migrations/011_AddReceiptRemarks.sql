-- Migration: 011_AddReceiptRemarks.sql
-- Purpose: Add remarks column to receipts table for Quick Pay notes

ALTER TABLE receipts ADD COLUMN IF NOT EXISTS remarks TEXT NOT NULL DEFAULT '';
