using AutoMapper;
using Fintech.Core.Domain;
using System;

namespace Fintech;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));
        CreateMap<RegisterRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

        // Customer
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.Aadhaar, opt => opt.MapFrom(src => Mask(src.Aadhaar_Encrypted)))
            .ForMember(dest => dest.Pan, opt => opt.MapFrom(src => Mask(src.PAN_Encrypted)));
        CreateMap<CustomerRequest, Customer>()
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
            .ForMember(dest => dest.Aadhaar_Encrypted, opt => opt.MapFrom(src => src.AadhaarEncrypted ?? src.Aadhaar ?? string.Empty))
            .ForMember(dest => dest.PAN_Encrypted, opt => opt.MapFrom(src => src.PanEncrypted ?? src.Pan ?? string.Empty));

        // Loan
        CreateMap<LoanCase, LoanCaseDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : ""));
        CreateMap<CreateLoanRequest, LoanCase>()
            .ForMember(d => d.TotalReceivable, opt => opt.MapFrom(s => s.Principal + s.InterestAmount));

        // Installment
        CreateMap<Installment, InstallmentDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        // Receipt
        CreateMap<Receipt, ReceiptDto>()
            .ForMember(d => d.Mode, opt => opt.MapFrom(s => s.Mode.ToString()));
        CreateMap<RecordPaymentRequest, Receipt>();

        // Partner
        CreateMap<Partner, PartnerDto>()
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.Phone, opt => opt.MapFrom(s => s.Phone));
        CreateMap<PartnerRequest, Partner>();

        // Capital (Mapped manually in service)
        CreateMap<CapitalAccount, PartnerCapitalSummaryDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();
    }

    private static string Mask(string input)
    {
        if (string.IsNullOrEmpty(input)) return "N/A";
        // Assuming encryption helper returns plaintext or we are mapping from encrypted string but we want to mask it properly
        // For simplicity, if it's already encrypted, we can't mask unless we decrypt first.
        // But the encryption is handled in DbContext. So here we might be receiving the decrypted value if EF is used.
        if (input.Length <= 4) return new string('*', input.Length);
        return new string('*', input.Length - 4) + input.Substring(input.Length - 4);
    }
}
