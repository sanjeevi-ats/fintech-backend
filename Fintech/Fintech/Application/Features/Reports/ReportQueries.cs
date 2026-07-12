using System;
using MediatR;

namespace Fintech.Application.Features.Reports;

public class ReportQueryBase
{
    public Guid BranchId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetPortfolioAtRiskQuery : ReportQueryBase, IRequest<object> { }
public class GetCollectionEfficiencyQuery : ReportQueryBase, IRequest<object> { }
public class GetTrialBalanceReportQuery : ReportQueryBase, IRequest<object> { }
public class GetDisbursementTrendsQuery : ReportQueryBase, IRequest<object> { }
public class GetAgentProductivityQuery : ReportQueryBase, IRequest<object> { }
