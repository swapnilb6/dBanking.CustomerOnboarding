using AutoMapper;
using Core.DTOS;
using Core.Entities;
using Core.Messages;
using Core.RepositoryContracts;
using Core.ServiceContracts;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics.Metrics;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private readonly IDistributedCache _distributedCache;

        public CustomerService(ICustomerRepository customers, IMapper mapper, IPublishEndpoint publishEndpoint
            , IKycCaseService kycCases,
            IAuditService audit,
            ICorrelationAccessor corr,
            IDistributedCache distributedCache)
        {
            _customers = customers;
            _publish = publishEndpoint;
            _kycCases = kycCases;
            _mapper = mapper;
            _audit = audit;
            _corr = corr;
            _distributedCache = distributedCache;
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


        public async Task<Customer?> GetAsync(Guid customerId, CancellationToken ct = default)
        {
            string cacheKey = $"customer:{customerId}";

            string? cachedValue = await _distributedCache.GetStringAsync(cacheKey);
            
            if (cachedValue != null)
            {
                return JsonSerializer.Deserialize<Customer>(cachedValue);
            }
            Customer? customer = null;
            try
            {

                 customer = await _customers.GetByIdAsync(customerId, ct);
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                // client aborted or gateway timed out
                //return StatusCode(499); // Client Closed Request (nginx convention) or 408/504
                throw;
            }
            if (customer != null)
            {
                DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30), // Cache for 30 minutes
                    SlidingExpiration = TimeSpan.FromMinutes(10) // Reset expiration on access
                };


                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };


                await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(customer), cacheOptions);
            }
            return customer;
        }

        public async Task<Customer?> GetByEmailOrPhoneAsync(string? email, string? phone, CancellationToken ct = default)
        {
            // Try cache first if email provided
            if (!string.IsNullOrEmpty(email))
            {
                string emailCacheKey = $"customer:email:{email}";
                string? cachedValue = await _distributedCache.GetStringAsync(emailCacheKey, ct);
                if (cachedValue != null)
                {
                    return JsonSerializer.Deserialize<Customer>(cachedValue);
                }
            }

            // Try cache first if phone provided
            if (!string.IsNullOrEmpty(phone))
            {
                string phoneCacheKey = $"customer:phone:{phone}";
                string? cachedValue = await _distributedCache.GetStringAsync(phoneCacheKey, ct);
                if (cachedValue != null)
                {
                    return JsonSerializer.Deserialize<Customer>(cachedValue);
                }
            }

            // Fetch from repository
            Customer? customer = await _customers.GetByEmailOrPhoneAsync(email, phone, ct);
            
            if (customer != null)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                };

                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                string serialized = JsonSerializer.Serialize(customer, options);

                // Cache by email if available
                if (!string.IsNullOrEmpty(email))
                {
                    await _distributedCache.SetStringAsync($"customer:email:{email}", serialized, cacheOptions, ct);
                }

                // Cache by phone if available
                if (!string.IsNullOrEmpty(phone))
                {
                    await _distributedCache.SetStringAsync($"customer:phone:{phone}", serialized, cacheOptions, ct);
                }
            }

            return customer;
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
