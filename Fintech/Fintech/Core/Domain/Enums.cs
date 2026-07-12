namespace Fintech.Core.Domain;

public enum UserRole
{
    super_admin = 0,
    branch_manager = 1,
    partner = 2,
    accountant = 3,
    collection_officer = 4,
    recovery_specialist = 5,
    loan_officer = 6,
    customer = 7,
    agent = 8
}

public enum Permission
{
    // User management
    VIEW_USERS = 0,
    MANAGE_USERS = 1,

    // Loan management
    VIEW_LOAN = 10,
    CREATE_LOAN = 11,
    DISBURSE_LOAN = 12,
    CLOSE_LOAN = 13,

    // Payment/Collection
    UPDATE_PAYMENT = 20,
    VIEW_COLLECTIONS = 21,

    // Reports/Accounting
    VIEW_REPORTS = 30,
    VIEW_TRANSACTIONS = 31,
    MANAGE_ACCOUNTS = 32,

    // Specialized
    MANAGE_BRANCHES = 40,
    RECOVERY_ACTIONS = 50
}

public enum LoanStatus
{
    pending_disburse = 0,
    active = 1,
    closed = 2,
    settled = 3,
    waived = 4,
    draft = 5,
    paid = 6,
    pending_approval = 7,
    rejected = 8
}

public enum InstallmentStatus
{
    pending = 0,
    paid = 1,
    overdue = 2,
    waived = 3,
    due = 4
}

public enum PaymentMode
{
    cash = 0,
    upi = 1,
    bank_transfer = 2
}

public enum Frequency
{
    monthly = 0,
    weekly = 1,
    daily_working = 2,
    bullet = 3
}

public enum JournalLineType
{
    debit = 0,
    credit = 1
}

public enum CollectionRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

