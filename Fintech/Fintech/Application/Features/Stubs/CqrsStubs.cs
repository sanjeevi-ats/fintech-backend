using System;
using MediatR;
using Fintech.Core.Domain;

namespace Fintech.Application.Features.Stubs;

// Auth Stubs
public class RefreshTokenCommand : IRequest<object> { public string Token { get; set; } = string.Empty; }
public class EnableTotpCommand : IRequest<object> { public Guid UserId { get; set; } }
public class ChangePasswordCommand : IRequest<object> { public Guid UserId { get; set; } public string OldPassword { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }

// Branch Stubs
public class GetBranchesQuery : IRequest<object> { }
public class SearchBranchesQuery : IRequest<object> { public string Query { get; set; } = string.Empty; }
public class UpdateBranchSettingsCommand : IRequest<object> { public Guid BranchId { get; set; } }
public class GetBranchStatsQuery : IRequest<object> { public Guid BranchId { get; set; } }

// Customer Stubs
public class CreateCustomerCommand : IRequest<object> { }
public class GetCustomerByIdQuery : IRequest<object> { public Guid CustomerId { get; set; } }
public class SearchCustomerByPhoneQuery : IRequest<object> { public string Phone { get; set; } = string.Empty; }

// Product Stubs
public class GetActiveProductsQuery : IRequest<object> { }
public class CreateLoanProductCommand : IRequest<object> { }
public class DeactivateProductCommand : IRequest<object> { public Guid ProductId { get; set; } }

// Loan Stubs
public class ApplyLoanCommand : IRequest<object> { }
public class ApproveLoanCommand : IRequest<object> { public Guid LoanId { get; set; } }

// Collection Stubs
public class WaiveFineCommand : IRequest<object> { public Guid InstallmentId { get; set; } }

// Ledger Stubs
public class GetJournalEntriesQuery : IRequest<object> { }
public class ManualJournalEntryCommand : IRequest<object> { }

// Partner Stubs
public class GetPartnerEquityQuery : IRequest<object> { }
public class GetPayoutHistoryQuery : IRequest<object> { }

// Audit Stubs
public class GetEntityHistoryQuery : IRequest<object> { public string EntityName { get; set; } = string.Empty; public string Id { get; set; } = string.Empty; }
public class GetSystemLogsQuery : IRequest<object> { }

// DayEnd Stubs
public class CloseDayEndCommand : IRequest<object> { }
public class GetDayEndStatusQuery : IRequest<object> { }
public class UpdateDayEndCommand : IRequest<object> { }

// Recovery Stubs
public class WaiveRecoveryFineCommand : IRequest<object> { }
public class GetOverdueLoansQuery : IRequest<object> { }
public class UpdateRecoveryCommand : IRequest<object> { public Guid RecoveryId { get; set; } }
public class DeleteRecoveryCommand : IRequest<object> { public Guid RecoveryId { get; set; } }
