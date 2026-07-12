import os
import re

replacements = {
    'LoanStatus.draft': '"draft"',
    'LoanStatus.pending_disburse': '"pending_disburse"',
    'LoanStatus.active': '"active"',
    'LoanStatus.closed': '"closed"',
    'TransactionType.debit': '"debit"',
    'TransactionType.credit': '"credit"',
    'InstallmentStatus.due': '"due"',
    'InstallmentStatus.paid': '"paid"',
    'InstallmentStatus.overdue': '"overdue"',
    'UserRole.super_admin': '"super_admin"',
    'UserRole.partner': '"partner"',
    'UserRole.branch_manager': '"branch_manager"',
    'UserRole.accountant': '"accountant"',
    'UserRole.collection_officer': '"collection_officer"',
    'UserRole.recovery_specialist': '"recovery_specialist"',
    'UserRole.loan_officer': '"loan_officer"'
}

for root, _, files in os.walk('.'):
    for f in files:
        if f.endswith('.cs'):
            path = os.path.join(root, f)
            with open(path, 'r') as file:
                content = file.read()
            original = content
            
            for k, v in replacements.items():
                content = content.replace(k, v)
                
            # Replace property types in Domain Models
            content = re.sub(r'public\s+LoanStatus', 'public string', content)
            content = re.sub(r'public\s+TransactionType', 'public string', content)
            content = re.sub(r'public\s+InstallmentStatus', 'public string', content)
            content = re.sub(r'public\s+UserRole', 'public string', content)
            
            # Remove enum variables if any are typed strongly
            content = re.sub(r'\bLoanStatus\b(?!\.)', 'string', content)
            content = re.sub(r'\bTransactionType\b(?!\.)', 'string', content)
            content = re.sub(r'\bInstallmentStatus\b(?!\.)', 'string', content)
            content = re.sub(r'\bUserRole\b(?!\.)', 'string', content)
            
            if content != original:
                with open(path, 'w') as file:
                    file.write(content)
