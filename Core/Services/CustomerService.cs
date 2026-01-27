using AutoMapper;
using Core.Entities;
using Core.Messages;
using Core.RepositoryContracts;
using Core.ServiceContracts;
using MassTransit;
using Core.DTOS;
using System.Diagnostics.Metrics;
using System.Security.Principal;

namespace Core.Services
{
    /// <summary>
    /// Application service for managing customer-related operations.
    /// </summary>  
    public sealed class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customers;
        private readonly IPublishEndpoint _publish;
        private readonly IMapper _mapper;
        private readonly IKycCaseService _kycCases;
        // TODO: Inject IAuditRepository (when implemented), IIdempotencyStore (e.g., Redis) if desired


        private readonly IAuditService _audit;
        private readonly ICorrelationAccessor _corr;
       
        public CustomerService(ICustomerRepository customers, IMapper mapper, IPublishEndpoint publishEndpoint
            , IKycCaseService kycCases,
            IAuditService audit,
            ICorrelationAccessor corr)
        {
            _customers = customers;
            _publish = publishEndpoint;
            _kycCases = kycCases;
            _mapper = mapper;
            _audit = audit;
            _corr = corr;

        }

        public async Task<Customer> CreateAsync(Customer input, string? idempotencyKey = null, CancellationToken ct = default)
        {
            // Optional: honor idempotency (hook point)
            // if (!string.IsNullOrEmpty(idempotencyKey))
            // {
            //     var existsForKey = await _idempotency.ExistsAsync(idempotencyKey, ct);
            //     if (existsForKey) return await _idempotency.GetResultAsync<Customer>(idempotencyKey, ct);
            // }

            // Basic dedupe by email/phone
            var duplicate = await _customers.ExistsByEmailOrPhoneAsync(input.Email, input.Phone, ct);
            if (duplicate)
                throw new InvalidOperationException("Duplicate customer detected. Email or phone already exists.");

            // Persist
            await _customers.AddAsync(input, ct);
            await _customers.SaveChangesAsync(ct);

            // Publish domain event

            await _publish.Publish<CustomerCreated>(new
            {
                CustomerId = input.CustomerId,
                input.FirstName,
                input.LastName,
                input.Email,
                CreatedAtUtc = input.CreatedAt,
                SourceSystem = "CustomerApi",
                CorrelationId = input.CustomerId
            }, ct);



            // Audit: CustomerCreated
            await _audit.RecordAsync(new Core.DTOS.AuditEntryDto(
                EntityType: "Customer",
                Action: AuditAction.Create,
                TargetEntityId: input.CustomerId,
                RelatedEntityId: null,
                Actor: "CustomerService",
                CorrelationId: _corr.Get(),
                BeforeSnapshot: null,
                AfterSnapshot: new { input.CustomerId, input.FirstName, input.LastName, input.Email, input.Phone, input.Status },
                Source: "API"
            ), ct);


            // Optional: store idempotency result
            // if (!string.IsNullOrEmpty(idempotencyKey))
            //     await _idempotency.StoreResultAsync(idempotencyKey, input, ct);



            // Auto-start KYC (idempotent; returns existing PENDING if any)
            await _kycCases.StartForCustomerAsync(
                new Core.DTOS.KycCaseCreateRequestDto(
                    CustomerId: input.CustomerId,
                    EvidenceRefs: new List<string>(),            // TODO: supply actual refs if available
                    ConsentText: "I consent to eKYC verification for account onboarding.",
                    AcceptedAt: DateTime.UtcNow,
                    IdempotencyKey: idempotencyKey
                ),
                ct);

            return input;
        }


        public Task<Customer?> GetAsync(Guid customerId, CancellationToken ct = default)
        {
            return _customers.GetByIdAsync(customerId, ct);
        }

        public Task<Customer?> GetByEmailOrPhoneAsync(string? email, string? phone, CancellationToken ct = default)
        {
            return _customers.GetByEmailOrPhoneAsync(email, phone, ct);
        }

        public async Task<Customer> UpdateAsync(Customer customer, CancellationToken ct = default)
        {
            // Ensure the entity exists before updating (optional safeguard)
            var existing = await _customers.GetByIdAsync(customer.CustomerId, ct);
            if (existing is null)
                throw new KeyNotFoundException($"Customer '{customer.CustomerId}' not found.");



            var before = new { customer.Email, customer.Phone, customer.Status };

            //// Example: only allow certain fields to change here; others via dedicated flows (contacts/address/etc.)
            //existing.FirstName = customer.FirstName;
            //existing.LastName = customer.LastName;
            //existing.Dob = customer.Dob;
            //existing.UpdatedAt = DateTime.UtcNow;

            // Alternatively, use AutoMapper to map allowed fields
            _mapper.Map(customer, existing);

            await _customers.UpdateAsync(existing, ct);
            await _customers.SaveChangesAsync(ct);

            // Optional: publish an update event (define it when needed)
            await _publish.Publish <Core.Messages.CustomerUpdated>(new
            {   existing.CustomerId, 
                existing.FirstName, 
                existing.LastName 
            });

            var after = new { customer.Email, customer.Phone, customer.Status };

            await _audit.RecordAsync(new DTOS.AuditEntryDto(
                        EntityType: "Customer",
                        Action: AuditAction.Update,
                        TargetEntityId: customer.CustomerId,
                        RelatedEntityId: null,
                        Actor: "CustomerService",
                        CorrelationId: _corr.Get(),
                        BeforeSnapshot: before,
                        AfterSnapshot: after,
                        Source: "API"
                    ), ct);

            return existing;
        }
    }

}
