import requests
import json
import time
from datetime import datetime
from typing import Dict, List, Tuple
import uuid

class APITestAutomation:
    def __init__(self, base_url: str = "http://localhost:5177"):
        self.base_url = base_url
        self.auth_token = None
        self.test_results = []
        self.created_resources = {
            "branches": [],
            "users": [],
            "customers": [],
            "loans": [],
            "partners": [],
            "products": []
        }
        
    def set_auth_token(self, token: str):
        """Set authentication token for subsequent requests"""
        self.auth_token = token
        
    def get_headers(self, include_auth: bool = True) -> Dict:
        """Get request headers"""
        headers = {"Content-Type": "application/json"}
        if include_auth and self.auth_token:
            headers["Authorization"] = f"Bearer {self.auth_token}"
        return headers
    
    def make_request(self, method: str, endpoint: str, payload=None, 
                     include_auth: bool = True, params: Dict = None) -> Tuple[int, Dict]:
        """Make HTTP request and return status code and response"""
        url = f"{self.base_url}{endpoint}"
        headers = self.get_headers(include_auth)
        
        try:
            if method == "GET":
                response = requests.get(url, headers=headers, params=params, timeout=30)
            elif method == "POST":
                response = requests.post(url, headers=headers, json=payload, timeout=30)
            elif method == "PUT":
                response = requests.put(url, headers=headers, json=payload, timeout=30)
            elif method == "DELETE":
                response = requests.delete(url, headers=headers, timeout=30)
            else:
                return 0, {"error": f"Unsupported method: {method}"}
            
            # Handle empty responses (204 No Content)
            if response.status_code == 204:
                return response.status_code, {}
            
            try:
                return response.status_code, response.json()
            except:
                return response.status_code, {"text": response.text}
        except requests.exceptions.RequestException as e:
            return 0, {"error": str(e)}
    
    def log_result(self, test_id: str, endpoint: str, description: str, 
                   expected_status: int, actual_status: int, 
                   response: Dict, passed: bool, error_msg: str = ""):
        """Log test result"""
        result = {
            "test_id": test_id,
            "endpoint": endpoint,
            "description": description,
            "expected_status": expected_status,
            "actual_status": actual_status,
            "response": response,
            "passed": passed,
            "error_msg": error_msg,
            "timestamp": datetime.now().isoformat()
        }
        self.test_results.append(result)
        
        status_icon = "✓" if passed else "✗"
        print(f"{status_icon} {test_id}: {description}")
        if not passed:
            print(f"  Expected: {expected_status}, Got: {actual_status}")
            if error_msg:
                print(f"  Error: {error_msg}")
    
    def generate_html_report(self, filename: str = "test_report.html"):
        """Generate HTML test report"""
        total = len(self.test_results)
        passed = sum(1 for r in self.test_results if r["passed"])
        failed = total - passed
        pass_rate = (passed / total * 100) if total > 0 else 0
        
        html = f"""<!DOCTYPE html>
<html>
<head>
    <title>API Test Automation Report</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 20px;
            background-color: #f5f5f5;
        }}
        .header {{
            background-color: #2c3e50;
            color: white;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 20px;
        }}
        .summary {{
            display: flex;
            gap: 20px;
            margin-bottom: 20px;
        }}
        .summary-card {{
            flex: 1;
            padding: 20px;
            border-radius: 5px;
            color: white;
            text-align: center;
        }}
        .total {{ background-color: #3498db; }}
        .passed {{ background-color: #27ae60; }}
        .failed {{ background-color: #e74c3c; }}
        .pass-rate {{ background-color: #9b59b6; }}
        .summary-card h2 {{
            margin: 0;
            font-size: 36px;
        }}
        .summary-card p {{
            margin: 5px 0 0 0;
            font-size: 14px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            background-color: white;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        th {{
            background-color: #34495e;
            color: white;
            padding: 12px;
            text-align: left;
        }}
        td {{
            padding: 10px;
            border-bottom: 1px solid #ddd;
        }}
        tr:hover {{
            background-color: #f5f5f5;
        }}
        .pass {{
            color: #27ae60;
            font-weight: bold;
        }}
        .fail {{
            color: #e74c3c;
            font-weight: bold;
        }}
        .error-msg {{
            color: #e74c3c;
            font-size: 12px;
            font-style: italic;
        }}
        .response {{
            max-width: 300px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            font-size: 12px;
            color: #7f8c8d;
        }}
    </style>
</head>
<body>
    <div class="header">
        <h1>API Test Automation Report</h1>
        <p>Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
    </div>
    
    <div class="summary">
        <div class="summary-card total">
            <h2>{total}</h2>
            <p>Total Tests</p>
        </div>
        <div class="summary-card passed">
            <h2>{passed}</h2>
            <p>Passed</p>
        </div>
        <div class="summary-card failed">
            <h2>{failed}</h2>
            <p>Failed</p>
        </div>
        <div class="summary-card pass-rate">
            <h2>{pass_rate:.1f}%</h2>
            <p>Pass Rate</p>
        </div>
    </div>
    
    <table>
        <thead>
            <tr>
                <th>Test ID</th>
                <th>Endpoint</th>
                <th>Description</th>
                <th>Expected</th>
                <th>Actual</th>
                <th>Status</th>
                <th>Details</th>
            </tr>
        </thead>
        <tbody>
"""
        
        for result in self.test_results:
            status_class = "pass" if result["passed"] else "fail"
            status_text = "PASS" if result["passed"] else "FAIL"
            error_html = f'<div class="error-msg">{result["error_msg"]}</div>' if result["error_msg"] else ""
            response_str = json.dumps(result["response"])[:100]
            
            html += f"""
            <tr>
                <td>{result["test_id"]}</td>
                <td>{result["endpoint"]}</td>
                <td>{result["description"]}</td>
                <td>{result["expected_status"]}</td>
                <td>{result["actual_status"]}</td>
                <td class="{status_class}">{status_text}</td>
                <td>
                    <div class="response">{response_str}</div>
                    {error_html}
                </td>
            </tr>
"""
        
        html += """
        </tbody>
    </table>
</body>
</html>
"""
        
        with open(filename, 'w', encoding='utf-8') as f:
            f.write(html)
        
        print(f"\n{'='*60}")
        print(f"HTML Report generated: {filename}")
        print(f"Total Tests: {total} | Passed: {passed} | Failed: {failed} | Pass Rate: {pass_rate:.1f}%")
        print(f"{'='*60}")


def run_positive_flow_tests():
    """Run all positive flow tests (200 status codes)"""
    
    # Sample GUIDs for testing
    SAMPLE_BRANCH_ID = str(uuid.uuid4())
    SAMPLE_CUSTOMER_ID = str(uuid.uuid4())
    SAMPLE_LOAN_ID = str(uuid.uuid4())
    SAMPLE_INSTALLMENT_ID = str(uuid.uuid4())
    SAMPLE_PARTNER_ID = str(uuid.uuid4())
    SAMPLE_USER_ID = str(uuid.uuid4())
    SAMPLE_PRODUCT_ID = str(uuid.uuid4())
    
    tester = APITestAutomation()
    
    print("="*60)
    print("Starting API Test Automation - Positive Flow Tests")
    print("="*60)
    print()
    
    # TS_API_001: Login with valid credentials
    print("Testing Authentication Endpoints...")
    status, response = tester.make_request(
        "POST", "/api/v1/auth/login",
        {"email": "admin@fintech.com", "password": "Admin@123"},
        include_auth=False
    )
    passed = status == 200 and "token" in response
    tester.log_result(
        "TS_API_001", "POST /api/v1/auth/login",
        "Verify login with valid credentials returns 200",
        200, status, response, passed,
        "" if passed else "Login failed or token not returned"
    )
    
    if passed and "token" in response:
        tester.set_auth_token(response["token"])
    
    # TS_API_006: Register new user
    status, response = tester.make_request(
        "POST", "/api/v1/auth/register",
        {
            "name": "John Doe",
            "email": f"john.doe.{int(time.time())}@fintech.com",
            "password": "SecurePass@123",
            "branchId": SAMPLE_BRANCH_ID,
            "role": "agent"
        },
        include_auth=False
    )
    passed = status == 200
    tester.log_result(
        "TS_API_006", "POST /api/v1/auth/register",
        "Verify register with valid data returns 200",
        200, status, response, passed
    )
    
    # TS_API_015: Get all branches
    print("\nTesting Branch Endpoints...")
    status, response = tester.make_request("GET", "/api/v1/branch", None)
    passed = status == 200
    tester.log_result(
        "TS_API_015", "GET /api/v1/branch",
        "Verify get all branches returns 200",
        200, status, response, passed
    )
    
    # Store first branch ID if available
    if passed and isinstance(response, list) and len(response) > 0:
        SAMPLE_BRANCH_ID = response[0].get("id", SAMPLE_BRANCH_ID)
    
    # TS_API_019: Create branch
    status, response = tester.make_request(
        "POST", "/api/v1/branch",
        {"name": f"Test Branch {int(time.time())}", "city": "Delhi", "isActive": True}
    )
    passed = status in [200, 201]
    tester.log_result(
        "TS_API_019", "POST /api/v1/branch",
        "Verify create branch returns 201",
        201, status, response, passed
    )
    
    if passed and isinstance(response, dict) and "id" in response:
        SAMPLE_BRANCH_ID = response["id"]
        tester.created_resources["branches"].append(SAMPLE_BRANCH_ID)
    
    # TS_API_016: Get branch by ID
    if SAMPLE_BRANCH_ID:
        status, response = tester.make_request("GET", f"/api/v1/branch/{SAMPLE_BRANCH_ID}", None)
        passed = status == 200
        tester.log_result(
            "TS_API_016", f"GET /api/v1/branch/{SAMPLE_BRANCH_ID}",
            "Verify get branch by ID returns 200",
            200, status, response, passed
        )
    
    # TS_API_027: Get all users
    print("\nTesting User Endpoints...")
    status, response = tester.make_request("GET", "/api/v1/users", None)
    passed = status == 200
    tester.log_result(
        "TS_API_027", "GET /api/v1/users",
        "Verify get all users returns 200",
        200, status, response, passed
    )
    
    # Store first user ID if available
    if passed and isinstance(response, list) and len(response) > 0:
        SAMPLE_USER_ID = response[0].get("id", SAMPLE_USER_ID)
    
    # TS_API_030: Create user
    status, response = tester.make_request(
        "POST", "/api/v1/users",
        {
            "name": "Jane Smith",
            "email": f"jane.smith.{int(time.time())}@fintech.com",
            "password": "SecurePass@123",
            "branchId": SAMPLE_BRANCH_ID,
            "role": "loan_officer"
        }
    )
    passed = status in [200, 201]
    tester.log_result(
        "TS_API_030", "POST /api/v1/users",
        "Verify create user returns 201",
        201, status, response, passed
    )
    
    if passed and isinstance(response, dict) and "id" in response:
        SAMPLE_USER_ID = response["id"]
        tester.created_resources["users"].append(SAMPLE_USER_ID)
    
    # TS_API_028: Get user by ID
    if SAMPLE_USER_ID:
        status, response = tester.make_request("GET", f"/api/v1/users/{SAMPLE_USER_ID}", None)
        passed = status == 200
        tester.log_result(
            "TS_API_028", f"GET /api/v1/users/{SAMPLE_USER_ID}",
            "Verify get user by ID returns 200",
            200, status, response, passed
        )
    
    # TS_API_034: Get all customers
    print("\nTesting Customer Endpoints...")
    status, response = tester.make_request("GET", "/api/v1/customers", None)
    passed = status == 200
    tester.log_result(
        "TS_API_034", "GET /api/v1/customers",
        "Verify get all customers returns 200",
        200, status, response, passed
    )
    
    # TS_API_037: Create customer
    status, response = tester.make_request(
        "POST", "/api/v1/customers",
        {
            "name": "Priya Sharma",
            "aadhaar": "123456789012",
            "pan": "ABCDE1234F",
            "branchId": SAMPLE_BRANCH_ID
        }
    )
    passed = status in [200, 201]
    tester.log_result(
        "TS_API_037", "POST /api/v1/customers",
        "Verify create customer returns 201",
        201, status, response, passed
    )
    
    if passed and isinstance(response, dict) and "id" in response:
        SAMPLE_CUSTOMER_ID = response["id"]
        tester.created_resources["customers"].append(SAMPLE_CUSTOMER_ID)
    
    # TS_API_035: Get customer by ID
    if SAMPLE_CUSTOMER_ID:
        status, response = tester.make_request("GET", f"/api/v1/customers/{SAMPLE_CUSTOMER_ID}", None)
        passed = status == 200
        tester.log_result(
            "TS_API_035", f"GET /api/v1/customers/{SAMPLE_CUSTOMER_ID}",
            "Verify get customer by ID returns 200",
            200, status, response, passed
        )
    
    # TS_API_042: Get all loan cases
    print("\nTesting Loan Case Endpoints...")
    status, response = tester.make_request("GET", "/api/v1/loancases", None)
    passed = status == 200
    tester.log_result(
        "TS_API_042", "GET /api/v1/loancases",
        "Verify get all loan cases returns 200",
        200, status, response, passed
    )
    
    # TS_API_045: Create loan case
    if SAMPLE_CUSTOMER_ID:
        status, response = tester.make_request(
            "POST", "/api/v1/loancases",
            {
                "customerId": SAMPLE_CUSTOMER_ID,
                "principal": 10000000,
                "interestAmount": 2000000,
                "processingFees": 100000
            }
        )
        passed = status in [200, 201]
        tester.log_result(
            "TS_API_045", "POST /api/v1/loancases",
            "Verify create loan case returns 201",
            201, status, response, passed
        )
        
        if passed and isinstance(response, dict) and "id" in response:
            SAMPLE_LOAN_ID = response["id"]
            tester.created_resources["loans"].append(SAMPLE_LOAN_ID)
    
    # TS_API_043: Get loan case by ID
    if SAMPLE_LOAN_ID:
        status, response = tester.make_request("GET", f"/api/v1/loancases/{SAMPLE_LOAN_ID}", None)
        passed = status == 200
        tester.log_result(
            "TS_API_043", f"GET /api/v1/loancases/{SAMPLE_LOAN_ID}",
            "Verify get loan case by ID returns 200",
            200, status, response, passed
        )
    
    # TS_API_066: Get all partners
    print("\nTesting Partner Endpoints...")
    status, response = tester.make_request("GET", "/api/v1/partners", None)
    passed = status == 200
    tester.log_result(
        "TS_API_066", "GET /api/v1/partners",
        "Verify get all partners returns 200",
        200, status, response, passed
    )
    
    # TS_API_069: Create partner
    status, response = tester.make_request(
        "POST", "/api/v1/partners",
        {
            "name": f"Partner Ltd {int(time.time())}",
            "email": f"partner.{int(time.time())}@company.com",
            "phone": "9123456789",
            "branchId": SAMPLE_BRANCH_ID
        }
    )
    passed = status in [200, 201]
    tester.log_result(
        "TS_API_069", "POST /api/v1/partners",
        "Verify create partner returns 201",
        201, status, response, passed
    )
    
    if passed and isinstance(response, dict) and "id" in response:
        SAMPLE_PARTNER_ID = response["id"]
        tester.created_resources["partners"].append(SAMPLE_PARTNER_ID)
    
    # TS_API_067: Get partner by ID
    if SAMPLE_PARTNER_ID:
        status, response = tester.make_request("GET", f"/api/v1/partners/{SAMPLE_PARTNER_ID}", None)
        passed = status == 200
        tester.log_result(
            "TS_API_067", f"GET /api/v1/partners/{SAMPLE_PARTNER_ID}",
            "Verify get partner by ID returns 200",
            200, status, response, passed
        )
    
    # TS_API_074: Add investment
    print("\nTesting Capital Account Endpoints...")
    if SAMPLE_PARTNER_ID:
        status, response = tester.make_request(
            "POST", "/api/v1/capitalaccounts/investment",
            {"partnerId": SAMPLE_PARTNER_ID, "amount": 50000000}
        )
        passed = status == 200
        tester.log_result(
            "TS_API_074", "POST /api/v1/capitalaccounts/investment",
            "Verify add investment returns 200",
            200, status, response, passed
        )
    
    # TS_API_081: Get capital summary
    if SAMPLE_PARTNER_ID:
        status, response = tester.make_request("GET", f"/api/v1/capitalaccounts/summary/{SAMPLE_PARTNER_ID}", None)
        passed = status == 200
        tester.log_result(
            "TS_API_081", f"GET /api/v1/capitalaccounts/summary/{SAMPLE_PARTNER_ID}",
            "Verify get capital summary returns 200",
            200, status, response, passed
        )
    
    # TS_API_083: Get active products
    print("\nTesting Product Endpoints...")
    status, response = tester.make_request("GET", "/api/v1/product/active", None)
    passed = status == 200
    tester.log_result(
        "TS_API_083", "GET /api/v1/product/active",
        "Verify get active products returns 200",
        200, status, response, passed
    )
    
    # TS_API_091: Get PAR report
    print("\nTesting Report Endpoints...")
    status, response = tester.make_request("GET", "/api/v1/report/par", None, params={"start": "2026-01-01", "end": "2026-04-11"})
    passed = status == 200
    tester.log_result(
        "TS_API_091", "GET /api/v1/report/par",
        "Verify get PAR report returns 200",
        200, status, response, passed
    )
    
    # TS_API_092: Get PAR report without dates
    status, response = tester.make_request("GET", "/api/v1/report/par", None)
    passed = status == 200
    tester.log_result(
        "TS_API_092", "GET /api/v1/report/par",
        "Verify get PAR report without dates returns 200",
        200, status, response, passed
    )
    
    # TS_API_093: Get collection efficiency
    status, response = tester.make_request("GET", "/api/v1/report/efficiency", None, params={"start": "2026-01-01", "end": "2026-04-11"})
    passed = status == 200
    tester.log_result(
        "TS_API_093", "GET /api/v1/report/efficiency",
        "Verify get collection efficiency returns 200",
        200, status, response, passed
    )
    
    # TS_API_095: Get dashboard stats
    status, response = tester.make_request("GET", "/api/v1/report/dashboard-stats", None)
    passed = status == 200
    tester.log_result(
        "TS_API_095", "GET /api/v1/report/dashboard-stats",
        "Verify get dashboard stats returns 200",
        200, status, response, passed
    )
    
    # TS_API_097: Get audit logs
    print("\nTesting Audit Endpoints...")
    if SAMPLE_LOAN_ID:
        status, response = tester.make_request("GET", f"/api/v1/audit/LoanCase/{SAMPLE_LOAN_ID}", None)
        passed = status == 200
        tester.log_result(
            "TS_API_097", f"GET /api/v1/audit/LoanCase/{SAMPLE_LOAN_ID}",
            "Verify get entity history returns 200",
            200, status, response, passed
        )
    
    # TS_API_099: Get recent logs
    status, response = tester.make_request("GET", "/api/v1/audit/recent", None, params={"count": 50})
    passed = status == 200
    tester.log_result(
        "TS_API_099", "GET /api/v1/audit/recent",
        "Verify get recent logs returns 200",
        200, status, response, passed
    )
    
    # TS_API_102: Get overdue loans
    print("\nTesting Recovery Endpoints...")
    status, response = tester.make_request("GET", "/api/recovery/overdue", None)
    passed = status == 200
    tester.log_result(
        "TS_API_102", "GET /api/recovery/overdue",
        "Verify get overdue loans returns 200",
        200, status, response, passed
    )
    
    # TS_API_108: Get journal entries
    print("\nTesting Journal Endpoints...")
    status, response = tester.make_request("GET", "/api/journal/entries", None)
    passed = status == 200
    tester.log_result(
        "TS_API_108", "GET /api/journal/entries",
        "Verify get journal entries returns 200",
        200, status, response, passed
    )
    
    # TS_API_109: Get trial balance
    print("\nTesting Ledger Endpoints...")
    status, response = tester.make_request("GET", "/api/ledger/trial-balance", None)
    passed = status == 200
    tester.log_result(
        "TS_API_109", "GET /api/ledger/trial-balance",
        "Verify get trial balance returns 200",
        200, status, response, passed
    )
    
    # TS_API_110: Get P&L
    status, response = tester.make_request("GET", "/api/ledger/pnl", None, params={"start": "2026-01-01", "end": "2026-04-11"})
    passed = status == 200
    tester.log_result(
        "TS_API_110", "GET /api/ledger/pnl",
        "Verify get P&L returns 200",
        200, status, response, passed
    )
    
    # Generate HTML report
    print("\n" + "="*60)
    print("Test Execution Completed!")
    print("="*60)
    tester.generate_html_report("api_test_report.html")
    
    return tester


if __name__ == "__main__":
    tester = run_positive_flow_tests()
