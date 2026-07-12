using Fintech.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers;

[AutoLog]
[Route("api/[controller]")]
public class CollectionController : BaseApiController
{
    private readonly ICollectionService _collectionService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<CollectionController> _logger;

    public CollectionController(
        ICollectionService collectionService, 
        ITenantService tenantService,
        ILogger<CollectionController> logger)
    {
        _collectionService = collectionService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpPost("collect")]
    public async Task<IActionResult> Collect([FromBody] RecordPaymentRequest request)
    {
        try
        {
            var role = _tenantService.UserRole;
            if (role != "super_admin" && role != "branch_manager")
            {
                return StatusCode(403, new { success = false, message = "Access Denied: Only Admins can record direct collection entries." });
            }

            var result = await _collectionService.CollectInstallmentAsync(request.InstallmentId, request.AmountPaid, request.Mode, request.UtrRef);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Collect");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while collecting payment.",
                Error = ex.Message
            });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] List<OfflineCollection> collections)
    {
        try
        {
            var result = await _collectionService.SyncOfflineCollectionsAsync(collections);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Sync");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An unexpected error occurred while syncing offline collections.",
                Error = ex.Message
            });
        }
    }
}
