using FluentValidation;

namespace Fintech;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    public CustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        // Accept either plain or encrypted Aadhaar — just ensure one is provided
        RuleFor(x => x.AadhaarEncrypted ?? x.Aadhaar)
            .NotEmpty().WithMessage("Aadhaar is required")
            .Must(v => v != null && v.Length >= 12).WithMessage("Aadhaar must be at least 12 characters");
        // Accept either plain or encrypted PAN — just ensure one is provided
        RuleFor(x => x.PanEncrypted ?? x.Pan)
            .NotEmpty().WithMessage("PAN is required")
            .Must(v => v != null && v.Length >= 5).WithMessage("PAN must be at least 5 characters");
    }
}

public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Principal).GreaterThan(0);
        RuleFor(x => x.InterestAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProcessingFees).GreaterThanOrEqualTo(0);
    }
}

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.InstallmentId).NotEmpty();
        RuleFor(x => x.AmountPaid).GreaterThan(0);
        RuleFor(x => x.Mode).NotEmpty().Must(x => new[] { "cash", "upi", "bank_transfer" }.Contains(x.ToLower()));
    }
}
