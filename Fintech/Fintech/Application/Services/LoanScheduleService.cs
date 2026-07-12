using System;
using System.Collections.Generic;
using Fintech.Core.Domain;

namespace Fintech.Application.Services;

public interface ILoanScheduleService
{
    List<Installment> GenerateSchedule(Guid loanCaseId, Guid branchId, int numInstallments, long initialInstallment, long restInstallment, DateTime startDate, long totalReceivable);
}

public class LoanScheduleService : ILoanScheduleService
{
    public List<Installment> GenerateSchedule(Guid loanCaseId, Guid branchId, int numInstallments, long initialInstallment, long restInstallment, DateTime startDate, long totalReceivable)
    {
        var installments = new List<Installment>();
        long runningTotal = 0;
        DateTime currentDate = startDate.Date;

        for (int i = 1; i <= numInstallments; i++)
        {
            // Sunday-Skip Rule
            if (currentDate.DayOfWeek == DayOfWeek.Sunday)
            {
                currentDate = currentDate.AddDays(1);
            }

            long amount = (i == 1) ? initialInstallment : restInstallment;
            
            installments.Add(new Installment
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                LoanCaseId = loanCaseId,
                No = i,
                DueDate = currentDate,
                Amount = amount,
                Status = InstallmentStatus.due
            });

            runningTotal += amount;
            currentDate = currentDate.AddDays(1);
        }

        if (runningTotal != totalReceivable)
        {
            throw new FinVedaException(422, "SCHEDULE_MISMATCH", $"Sum of installments ({runningTotal}) does not match TotalReceivable ({totalReceivable}).");
        }

        return installments;
    }
}
