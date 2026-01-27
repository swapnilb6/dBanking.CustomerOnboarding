using Core.DTOS;
using Core.Entities;
using Core.ServiceContracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dBanking.CustomerOnbaording.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class KycController : ControllerBase
    {
        private readonly IKycCaseService _kyc;

        public KycController(IKycCaseService kyc) => _kyc = kyc;

        /// <summary>
        /// Start a new KYC case (idempotent: returns existing PENDING case if present).
        /// Requires scope: kyc:write
        /// </summary>
        [HttpPost("customers/{customerId:guid}/start")]
        [Authorize(Policy = "KycWrite")]
        public async Task<ActionResult<KycCaseResponseDto>> Start(Guid customerId, [FromBody] KycCaseCreateRequestDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Ensure route and body consistency
            if (dto.CustomerId != customerId)
                return BadRequest("CustomerId in route and body must match.");

            var entity = await _kyc.StartForCustomerAsync(dto, ct);
            var resp = MapResponse(entity);
            return Ok(resp);
        }

        /// <summary>
        /// Get a KYC case by id.
        /// Requires scope: customer:read
        /// </summary>
        [HttpGet("cases/{kycCaseId:guid}")]
        [Authorize(Policy = "CustomerRead")]
        public async Task<ActionResult<KycCaseResponseDto>> GetById(Guid kycCaseId, CancellationToken ct)
        {
            var entity = await _kyc.GetByIdAsync(kycCaseId, ct);
            if (entity is null) return NotFound();
            return Ok(MapResponse(entity));
        }

        /// <summary>
        /// List KYC cases for a customer (summary).
        /// Requires scope: customer:read
        /// </summary>
        [HttpGet("customers/{customerId:guid}/cases")]
        [Authorize(Policy = "CustomerRead")]
        public async Task<ActionResult<List<KycCaseSummaryDto>>> GetForCustomer(Guid customerId, CancellationToken ct)
        {
            var list = await _kyc.GetByCustomerAsync(customerId, ct);
            var summaries = list.Select(MapSummary).ToList();
            return Ok(summaries);
        }

        /// <summary>
        /// Update KYC status (provider/back-office). Allowed transitions: PENDING->VERIFIED/FAILED.
        /// Requires scope: kyc:write
        /// </summary>
        [HttpPut("cases/{kycCaseId:guid}/status")]
        [Authorize(Policy = "KycWrite")]
        public async Task<ActionResult<KycCaseResponseDto>> UpdateStatus(Guid kycCaseId, [FromBody] KycStatusUpdateRequestDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (dto.KycCaseId != kycCaseId) return BadRequest("KycCaseId in route and body must match.");

            try
            {
                var updated = await _kyc.UpdateStatusAsync(dto, ct);
                return Ok(MapResponse(updated));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (ValidationException vex)
            {
                return Problem("Validation error", vex.Message, StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException ioex)
            {
                return Problem("Invalid state transition", ioex.Message, StatusCodes.Status409Conflict);
            }
        }

        /// <summary>
        /// External provider callback/webhook. Secure via signature/secret.
        /// </summary>
        [HttpPost("callback")]
        [AllowAnonymous] // Replace with [Authorize(Policy = "KycCallback")] when you add signature verification
        public async Task<IActionResult> Callback([FromBody] KycStatusUpdateRequestDto dto, CancellationToken ct)
        {
            // TODO: verify provider signature/secret before allowing
            await _kyc.UpdateStatusAsync(dto, ct);
            return Ok();
        }

        #region Mapping helpers (use AutoMapper if you prefer)
        private static KycCaseResponseDto MapResponse(KycCase e)
        {
            var statusDto = e.Status switch
            {
                KycStatus.PENDING => KycStatusDto.PENDING,
                KycStatus.VERIFIED => KycStatusDto.VERIFIED,
                KycStatus.FAILED => KycStatusDto.FAILED,
                _ => KycStatusDto.PENDING
            };

            var evidence = !string.IsNullOrWhiteSpace(e.EvidenceRefsJson)
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(e.EvidenceRefsJson) ?? 
                new List<string>(): new List<string>();

            return new KycCaseResponseDto(
                e.KycCaseId,
                e.CustomerId,
                statusDto,
                e.ProviderRef,
                evidence,
                e.ConsentText,
                e.AcceptedAt,
                e.CreatedAt,
                e.CheckedAt
            );
        }

        private static KycCaseSummaryDto MapSummary(KycCase e)
        {
            var statusDto = e.Status switch
            {
                KycStatus.PENDING => KycStatusDto.PENDING,
                KycStatus.VERIFIED => KycStatusDto.VERIFIED,
                KycStatus.FAILED => KycStatusDto.FAILED,
                _ => KycStatusDto.PENDING
            };

            return new KycCaseSummaryDto(
                e.KycCaseId,
                statusDto,
                e.ProviderRef,
                e.CreatedAt,
                e.CheckedAt
            );
        }
        #endregion
    }
}
