using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fintech.Application.Services;

namespace Fintech.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ReconciliationController : ControllerBase
    {
        private readonly BankReconciliationService _bankRecon;
        private readonly LoanLedgerReconciliationService _loanRecon;
        private readonly CollectionReconciliationService _collectionRecon;

        public ReconciliationController(
            BankReconciliationService bankRecon,
            LoanLedgerReconciliationService loanRecon,
            CollectionReconciliationService collectionRecon)
        {
            _bankRecon = bankRecon;
            _loanRecon = loanRecon;
            _collectionRecon = collectionRecon;
        }

        [HttpPost("bank/{periodId}")]
        [Authorize(Roles = "super_admin,branch_manager")]
        public async Task<IActionResult> ReconcileBank(Guid periodId, CancellationToken ct)
        {
            var result = await _bankRecon.ReconcileBankAsync(periodId, ct);
            return Ok(result);
        }

        [HttpPost("loans/{periodId}")]
        [Authorize(Roles = "super_admin,branch_manager")]
        public async Task<IActionResult> ReconcileLoans(Guid periodId, CancellationToken ct)
        {
            var result = await _loanRecon.ReconcileLoansAsync(periodId, ct);
            return Ok(result);
        }

        [HttpPost("collections/{periodId}")]
        [Authorize(Roles = "super_admin,branch_manager")]
        public async Task<IActionResult> ReconcileCollections(Guid periodId, CancellationToken ct)
        {
            var result = await _collectionRecon.ReconcileCollectionsAsync(periodId, ct);
            return Ok(result);
        }
    }
}
