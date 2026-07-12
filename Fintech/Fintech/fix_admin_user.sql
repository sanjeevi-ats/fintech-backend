-- FIX ADMIN USER - Add admin@finveda.com with correct credentials
-- Run this against your PostgreSQL database

-- Delete existing admin@finveda.com if exists
DELETE FROM users WHERE email = 'admin@finveda.com';

-- Insert new admin user with email: admin@finveda.com
INSERT INTO users (
    id, 
    branch_id, 
    name, 
    email, 
    password_hash, 
    role, 
    is_active, 
    totp_enabled, 
    refresh_token, 
    user_code,
    created_at,
    updated_at
) VALUES (
    '22222222-2222-2222-2222-222222222230',  -- Unique ID
    '11111111-1111-1111-1111-111111111111',  -- Main branch
    'Admin User',                             -- Name
    'admin@finveda.com',                      -- Email (lowercase, no underscore)
    'Admin@123',                              -- Password (plain text - backend will hash)
    'super_admin',                            -- Role
    true,                                     -- Is active
    false,                                    -- TOTP disabled
    '',                                       -- No refresh token
    'USR0010',                                -- User code
    NOW(),                                    -- Created at
    NOW()                                     -- Updated at
);

-- Also add other commonly used emails
INSERT INTO users (id, branch_id, name, email, password_hash, role, is_active, totp_enabled, refresh_token, user_code, created_at, updated_at) VALUES
('22222222-2222-2222-2222-222222222231', '11111111-1111-1111-1111-111111111111', 'Loan Officer', 'loan.officer@finveda.com', 'Loan@123', 'loan_officer', true, false, '', 'USR0011', NOW(), NOW()),
('22222222-2222-2222-2222-222222222232', '11111111-1111-1111-1111-111111111111', 'Collection Officer', 'collection.officer@finveda.com', 'Collection@123', 'collection_officer', true, false, '', 'USR0012', NOW(), NOW()),
('22222222-2222-2222-2222-222222222233', '11111111-1111-1111-1111-111111111111', 'Accountant', 'accountant@finveda.com', 'Account@123', 'accountant', true, false, '', 'USR0013', NOW(), NOW())
ON CONFLICT (email) DO NOTHING;

-- Verify users created
SELECT id, name, email, role, is_active, user_code 
FROM users 
WHERE email IN ('admin@finveda.com', 'loan.officer@finveda.com', 'collection.officer@finveda.com', 'accountant@finveda.com')
ORDER BY email;
