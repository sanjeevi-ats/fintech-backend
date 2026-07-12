using System;
using MediatR;
using Fintech.Core.Domain;

namespace Fintech.Application.Features.Collections.Commands;

public class CollectPaymentCommand : IRequest<Receipt>
{
    public Guid InstallmentId { get; set; }
    public long AmountPaid { get; set; }
    public PaymentMode Mode { get; set; }
    public string UTRRef { get; set; } = string.Empty;
    public double GPS_Lat { get; set; }
    public double GPS_Lng { get; set; }
}
