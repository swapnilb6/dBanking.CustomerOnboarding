using Core.DTOS;
using Core.Entities;
using Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace dBanking.CustomerOnbaording.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public sealed class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customers;

        public CustomersController(ICustomerService customers)
        {
            _customers = customers;
        }

        /// <summary>
        /// Create a new customer (status = PENDING_KYC).
        /// Requires scope: customer:write
        /// </summary>
        [HttpPost]
        //[Authorize(Policy = "App.write")]
        //[Authorize]
        public async Task<ActionResult<CustomerResponseDto>> CreateCustomer(
            [FromBody] CustomerCreateRequestDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Map DTO -> domain entity
            var entity = new Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Dob = dto.Dob,
                Email = dto.Email,
                Phone = dto.Phone,
                Status = CustomerStatus.PENDING_KYC,
                CreatedAt = DateTime.UtcNow
            };

            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();

            try
            {
                var created = await _customers.CreateAsync(entity, idempotencyKey, ct);
                var response = ToCustomerResponseDto(created);
                return CreatedAtAction(nameof(GetCustomerById), new { customerId = created.CustomerId }, response);
            }
            catch (InvalidOperationException ex) // duplicate, business rule violation, etc.
            {
                return Problem(
                    title: "Duplicate customer detected",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        /// <summary>
        /// Get customer by id (includes KYC case summaries if any).
        /// Requires scope: customer:read
        /// </summary>
        [HttpGet("{customerId:guid}")]
        //[Authorize(Policy = "App.read")]
        //[Authorize]
        public async Task<ActionResult<CustomerResponseDto>> GetCustomerById(Guid customerId, CancellationToken ct)
        {


            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // app-level timeout

            var entity = await _customers.GetAsync(customerId, cts.Token);
            if (entity is null) return NotFound();

            var response = ToCustomerResponseDto(entity);
            return Ok(response);
        }

        /// <summary>
        /// Search by email/phone (one or both). Returns a single match (if any).
        /// Requires scope: customer:read
        /// </summary>
        [HttpGet("search")]
        //[Authorize(Policy = "App.read")]
        //[Authorize]
        public async Task<ActionResult<CustomerResponseDto>> Search(
            [FromQuery] string? email,
            [FromQuery] string? phone,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest("Provide at least 'email' or 'phone' as query parameter.");
            }

            var entity = await _customers.GetByEmailOrPhoneAsync(email, phone, ct);
            if (entity is null) return NotFound();

            var response = ToCustomerResponseDto(entity);
            return Ok(response);
        }

        /// <summary>
        /// Update simple customer fields (firstName/lastName/dob).
        /// Use specific flows for contacts/address/preferences in Operation 2.
        /// Requires scope: customer:write
        /// </summary>
        [HttpPatch("{customerId:guid}")]
        //[Authorize(Policy = "App.write")]
        //[Authorize]
        public async Task<ActionResult<CustomerResponseDto>> UpdateCustomer(
            Guid customerId,
            [FromBody] CustomerUpdateRequestDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Map DTO -> domain (partial); service will fetch existing and apply changes safely
            var entity = new Customer
            {
                CustomerId = customerId,
                FirstName = dto.FirstName ?? string.Empty,
                LastName = dto.LastName ?? string.Empty,
                Dob = (DateOnly)dto.Dob,
            };

            try
            {
                var updated = await _customers.UpdateAsync(entity, ct);
                var response = ToCustomerResponseDto(updated);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ValidationException vex)
            {
                return Problem(
                    title: "Validation error",
                    detail: vex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        #region Mapping helpers

        private static CustomerResponseDto ToCustomerResponseDto(Customer e)
        {
            var statusDto = e.Status switch
            {
                CustomerStatus.PENDING_KYC => CustomerStatusDto.PENDING_KYC,
                CustomerStatus.VERIFIED => CustomerStatusDto.VERIFIED,
                CustomerStatus.CLOSED => CustomerStatusDto.CLOSED,
                _ => CustomerStatusDto.PENDING_KYC
            };

            // Map KYC cases to lightweight summaries; guard null
            var cases = e.KycCases is { Count: > 0 }
                ? e.KycCases.Select(k => new KycCaseSummaryDto(
                    KycCaseId: k.KycCaseId,
                    Status: k.Status switch
                    {
                        KycStatus.PENDING => KycStatusDto.PENDING,
                        KycStatus.VERIFIED => KycStatusDto.VERIFIED,
                        KycStatus.FAILED => KycStatusDto.FAILED,
                        _ => KycStatusDto.PENDING
                    },
                    ProviderRef: k.ProviderRef,
                    CreatedAt: k.CreatedAt,
                    CheckedAt: k.CheckedAt
                  )).ToList()
                : new List<KycCaseSummaryDto>();

            return new CustomerResponseDto(
                CustomerId: e.CustomerId,
                FirstName: e.FirstName,
                LastName: e.LastName,
                Dob: e.Dob,
                Email: e.Email,
                Phone: e.Phone,
                Status: statusDto,
                CreatedAt: e.CreatedAt,
                UpdatedAt: e.UpdatedAt,
                KycCases: cases
            );
        }

        #endregion
    }

}
