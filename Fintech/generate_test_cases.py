import pandas as pd
import json

def generate_test_cases():
    base_url = "http://localhost:5177"
    test_cases = []
    tc_id_counter = 1

    def add_tc(endpoint, description, payload, expected_status, expected_response):
        nonlocal tc_id_counter
        test_cases.append({
            "Testcase id": f"ts_{tc_id_counter:03d}",
            "Endpoint Name": f"{base_url}{endpoint}",
            "Test Case Description": description,
            "Request Payload": json.dumps(payload, indent=2) if payload else "{}",
            "Expected Status Code": expected_status,
            "Expected Response": expected_response,
            "Status": "",
            "Comments": ""
        })
        tc_id_counter += 1

    # --- Audit ---
    # GET /api/v1/Audit/{entityName}/{id}
    add_tc("/api/v1/Audit/User/00000000-0000-0000-0000-000000000000", "Verify whether the Audit API returns success for a valid entity and ID.", None, 200, "Success response with audit logs")
    add_tc("/api/v1/Audit/UnknownEntity/00000000-0000-0000-0000-000000000000", "Verify whether the Audit API returns empty for a non-existent entity.", None, 200, "Empty list or error message")
    
    # GET /api/v1/Audit/logs
    add_tc("/api/v1/Audit/logs", "Verify whether the Audit API returns all logs successfully.", None, 200, "Success response with list of all logs")

    # --- Auth ---
    # POST /api/v1/Auth/login
    add_tc("/api/v1/Auth/login", "Verify whether the Login API returns success message when the user provided a valid request payload.", {"email": "user@example.com", "password": "Password123"}, 200, "Success message with AccessToken and RefreshToken")
    add_tc("/api/v1/Auth/login", "Verify whether the Login API returns unauthorized for invalid credentials.", {"email": "user@example.com", "password": "WrongPassword"}, 401, "Error message: Unauthorized")
    add_tc("/api/v1/Auth/login", "Verify whether the Login API returns bad request for missing email.", {"password": "Password123"}, 400, "Error message: Email is required")
    
    # POST /api/v1/Auth/register
    add_tc("/api/v1/Auth/register", "Verify whether the Register API returns success message when valid details are provided.", {"name": "Test User", "email": "newuser@example.com", "password": "Password123", "branchId": "00000000-0000-0000-0000-000000000001", "role": 1}, 200, "Success message: User registered")
    add_tc("/api/v1/Auth/register", "Verify whether the Register API returns error for existing email.", {"name": "Test User", "email": "user@example.com", "password": "Password123", "branchId": "00000000-0000-0000-0000-000000000001", "role": 1}, 400, "Error message: Email already exists")
    
    # POST /api/v1/Auth/refresh-token
    add_tc("/api/v1/Auth/refresh-token", "Verify whether the Refresh Token API returns new tokens for a valid refresh token.", {"token": "valid_refresh_token"}, 200, "New AccessToken and RefreshToken")
    
    # POST /api/v1/Auth/enable-totp
    add_tc("/api/v1/Auth/enable-totp", "Verify whether the Enable TOTP API returns success for a valid user ID.", {"userId": "00000000-0000-0000-0000-000000000001"}, 200, "Success message and TOTP setup details")
    
    # POST /api/v1/Auth/change-password
    add_tc("/api/v1/Auth/change-password", "Verify whether the Change Password API returns success when valid credentials are provided.", {"userId": "00000000-0000-0000-0000-000000000001", "oldPassword": "Password123", "newPassword": "NewPassword123"}, 200, "Success message: Password changed")

    # --- Branch ---
    # GET /api/v1/Branch
    add_tc("/api/v1/Branch", "Verify whether the Branch API returns list of branches.", None, 200, "List of branches")
    
    # PUT /api/v1/Branch/{id}/settings
    add_tc("/api/v1/Branch/00000000-0000-0000-0000-000000000001/settings", "Verify whether the Branch Settings API updates successfully.", {"branchId": "00000000-0000-0000-0000-000000000001"}, 200, "Success message: Settings updated")
    
    # GET /api/v1/Branch/{id}/stats
    add_tc("/api/v1/Branch/00000000-0000-0000-0000-000000000001/stats", "Verify whether the Branch Stats API returns stats for a valid branch ID.", None, 200, "Branch statistics")

    # --- Collection ---
    # GET /api/v1/Collection/dcs
    add_tc("/api/v1/Collection/dcs", "Verify whether the Collection DCS API returns data correlation services info.", None, 200, "DCS details")
    
    # POST /api/v1/Collection/collect
    add_tc("/api/v1/Collection/collect", "Verify whether the Collect Payment API returns success for a valid payload.", {"installmentId": "00000000-0000-0000-0000-000000000001", "amountPaid": 1000, "mode": "Cash", "utrRef": "UTR123", "gpS_Lat": 12.9716, "gpS_Lng": 77.5946}, 200, "Payment collected successfully")
    add_tc("/api/v1/Collection/collect", "Verify whether the Collect Payment API returns error for negative amount.", {"installmentId": "00000000-0000-0000-0000-000000000001", "amountPaid": -100, "mode": "Cash"}, 400, "Error message: Amount cannot be negative")
    
    # POST /api/v1/Collection/{id}/waive-fine
    add_tc("/api/v1/Collection/00000000-0000-0000-0000-000000000001/waive-fine", "Verify whether the Waive Fine API returns success for a valid ID.", None, 200, "Success message: Fine waived")

    # --- Customer ---
    # POST /api/v1/Customer
    add_tc("/api/v1/Customer", "Verify whether the Create Customer API returns success with valid data.", {}, 200, "Customer created successfully")
    
    # GET /api/v1/Customer/{id}
    add_tc("/api/v1/Customer/00000000-0000-0000-0000-000000000001", "Verify whether the Get Customer API returns details for a valid ID.", None, 200, "Customer details")
    
    # GET /api/v1/Customer/search?phone=...
    add_tc("/api/v1/Customer/search?phone=9876543210", "Verify whether the Search Customer API returns results for a valid phone number.", None, 200, "Customer search results")

    # --- DayEnd ---
    # POST /api/v1/DayEnd/close
    add_tc("/api/v1/DayEnd/close", "Verify whether the Day End Close API returns success.", None, 200, "Day closed successfully")
    
    # GET /api/v1/DayEnd/status
    add_tc("/api/v1/DayEnd/status", "Verify whether the Day End Status API returns current status.", None, 200, "Day end status")
    
    # PUT /api/v1/DayEnd/update
    add_tc("/api/v1/DayEnd/update", "Verify whether the Day End Update API returns success with empty payload.", {}, 200, "Day end updated")

    # --- Ledger ---
    # GET /api/v1/Ledger/entries
    add_tc("/api/v1/Ledger/entries", "Verify whether the Ledger Entries API returns success.", None, 200, "List of ledger entries")
    
    # GET /api/v1/Ledger/balance-sheet
    add_tc("/api/v1/Ledger/balance-sheet", "Verify whether the Balance Sheet API returns success.", None, 200, "Balance sheet report")
    
    # POST /api/v1/Ledger/manual-entry
    add_tc("/api/v1/Ledger/manual-entry", "Verify whether the Manual Journal Entry API returns success.", {}, 200, "Entry recorded successfully")

    # --- Loan ---
    # POST /api/v1/Loan/apply
    add_tc("/api/v1/Loan/apply", "Verify whether the Apply Loan API returns success.", {}, 200, "Application received")
    
    # POST /api/v1/Loan/00000000-0000-0000-0000-000000000001/approve
    add_tc("/api/v1/Loan/00000000-0000-0000-0000-000000000001/approve", "Verify whether the Approve Loan API returns success with valid payload.", {"loanId": "00000000-0000-0000-0000-000000000001"}, 200, "Loan approved")
    
    # POST /api/v1/Loan/00000000-0000-0000-0000-000000000001/disburse
    add_tc("/api/v1/Loan/00000000-0000-0000-0000-000000000001/disburse", "Verify whether the Disburse Loan API returns success.", None, 200, "Loan disbursed")

    # --- Partner ---
    # GET /api/v1/Partner/equity
    add_tc("/api/v1/Partner/equity", "Verify whether the Partner Equity API returns success.", None, 200, "Equity details")
    
    # POST /api/v1/Partner/distribute
    add_tc("/api/v1/Partner/distribute", "Verify whether the Distribute Profits API returns success.", None, 200, "Profits distributed")
    
    # GET /api/v1/Partner/payouts
    add_tc("/api/v1/Partner/payouts", "Verify whether the Partner Payouts API returns success.", None, 200, "Payout records")

    # --- Product ---
    # GET /api/v1/Product/active
    add_tc("/api/v1/Product/active", "Verify whether the Active Products API returns success.", None, 200, "Active products list")
    
    # POST /api/v1/Product
    add_tc("/api/v1/Product", "Verify whether the Create Product API returns success.", {}, 200, "Product created")
    
    # DELETE /api/v1/Product/{id}
    add_tc("/api/v1/Product/00000000-0000-0000-0000-000000000001", "Verify whether the Delete Product API returns success for valid ID.", None, 200, "Product deleted")

    # --- Recovery ---
    # POST /api/v1/Recovery/waive
    add_tc("/api/v1/Recovery/waive", "Verify whether the Waive Recovery Fine API returns success.", {}, 200, "Fine waived")
    
    # GET /api/v1/Recovery/overdue
    add_tc("/api/v1/Recovery/overdue", "Verify whether the Overdue Recovery API returns success.", None, 200, "List of overdue recoveries")
    
    # PUT /api/v1/Recovery/00000000-0000-0000-0000-000000000001
    add_tc("/api/v1/Recovery/00000000-0000-0000-0000-000000000001", "Verify whether the Update Recovery API returns success.", {"recoveryId": "00000000-0000-0000-0000-000000000001"}, 200, "Recovery updated")
    
    # DELETE /api/v1/Recovery/00000000-0000-0000-0000-000000000001
    add_tc("/api/v1/Recovery/00000000-0000-0000-0000-000000000001", "Verify whether the Delete Recovery API returns success.", None, 200, "Recovery record deleted")

    # --- Reports ---
    report_endpoints = ["par", "efficiency", "trial-balance", "cash-flow", "agent-productivity"]
    for rep in report_endpoints:
        add_tc(f"/api/v1/Report/{rep}?startDate=2026-01-01&endDate=2026-12-31", f"Verify whether the {rep.title()} Report API returns success for valid dates.", None, 200, f"{rep.title()} report data")
        add_tc(f"/api/v1/Report/{rep}?startDate=invalid&endDate=2026-12-31", f"Verify whether the {rep.title()} Report API returns error for invalid date format.", None, 400, "Error message: Invalid date format")

    # Extra Negative/Data Type Cases
    add_tc("/api/v1/Auth/register", "Verify whether the Register API returns error when invalid data type is provided for Role.", {"name": "Test User", "email": "valid@example.com", "password": "Pass", "role": "INVALID_TYPE"}, 400, "Error message: Invalid data type for Role")
    add_tc("/api/v1/Collection/collect", "Verify whether the Collect Payment API returns error for missing installmentId.", {"amountPaid": 1000, "mode": "Cash"}, 400, "Error message: installmentId is required")

    # Convert to DataFrame and Export
    df = pd.DataFrame(test_cases)
    output_file = "FinVeda_API_TestCases.xlsx"
    df.to_excel(output_file, index=False)
    print(f"Test cases generated successfully in {output_file}")

if __name__ == "__main__":
    generate_test_cases()
