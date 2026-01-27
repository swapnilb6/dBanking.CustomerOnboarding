
using AutoMapper;
using Core.DTOS;
using Core.Entities;
using Core.Messages;
using Core.RepositoryContracts;
using Core.ServiceContracts;
using FluentValidation;
using MassTransit;
using System.Diagnostics.Metrics;
using System.Security.Principal;
using System.Text.Json;

namespace dBanking.Core.Services
{
    public sealed class KycCaseService : IKycCaseService
    {
        private readonly IKycCaseRepository _kycCases;
        private readonly ICustomerRepository _customers;
        private readonly IPublishEndpoint _publish;
        private readonly IMapper _mapper;

        private readonly IAuditService _audit;
        private readonly ICorrelationAccessor _corr;

        public KycCaseService(
            IKycCaseRepository kycCases,
            ICustomerRepository customers,
            IMapper mapper,
            IPublishEndpoint publish,
            IAuditService audit, ICorrelationAccessor corr)
        {
            _kycCases = kycCases;
            _customers = customers;
            _mapper = mapper;
            _publish = publish;
            _audit = audit;
            _corr = corr;
        }

        public async Task<KycCase> StartForCustomerAsync(KycCaseCreateRequestDto dto, CancellationToken ct)
        {
            // Idempotent start: if already PENDING case exists, return it
            var existing = await _kycCases.FindOpenForCustomerAsync(dto.CustomerId, ct);
            if (existing is not null) return existing;

            // Ensure customer exists
            var customer = await _customers.GetByIdAsync(dto.CustomerId, ct);
            if (customer is null)
                throw new KeyNotFoundException($"Customer '{dto.CustomerId}' not found.");
            try
            {
                // Map DTO -> entity
                var entity = _mapper.Map<KycCase>(dto);


                // Assign keys managed by service
                entity.KycCaseId = Guid.NewGuid();
                entity.CustomerId = dto.CustomerId; // or from route param if you pass it separately


                await _kycCases.AddAsync(entity, ct);
                await _kycCases.SaveChangesAsync(ct);



                // (Optional) Audit: create KYC case
                // await _audit.RecordAsync(...)

                // Audit: KycStarted (with consent/evidence refs)
                await _audit.RecordAsync(new AuditEntryDto(
                EntityType: "KycCase",
                Action: AuditAction.KycStarted,
                TargetEntityId: entity.KycCaseId,
                RelatedEntityId: customer.CustomerId,
                Actor: "KycCaseService",
                CorrelationId: _corr.Get(),
                BeforeSnapshot: null,
                AfterSnapshot: new { entity.KycCaseId, entity.CustomerId, entity.Status, entity.ProviderRef, EvidenceRefs = dto.EvidenceRefs, entity.ConsentText, entity.AcceptedAt },
                Source: "API"
            ), ct);
                return entity;

            }
            catch (Exception ex)
            {
                throw new ValidationException("An open KYC case already exists for this customer.");
            }

        }

        public async Task<KycCase> UpdateStatusAsync(KycStatusUpdateRequestDto dto, CancellationToken ct)
        {
            var caseEntity = await _kycCases.GetByIdAsync(dto.KycCaseId, ct);
            if (caseEntity is null)
                throw new KeyNotFoundException($"KYC case '{dto.KycCaseId}' not found.");

            if (caseEntity.CustomerId != dto.CustomerId)
                throw new ValidationException("CustomerId mismatch for provided KycCaseId.");

            var target = MapDtoStatus(dto.Status);

            // Terminal guard
            if (caseEntity.Status == KycStatus.VERIFIED || caseEntity.Status == KycStatus.FAILED)
                throw new InvalidOperationException("Cannot update a terminal KYC case (VERIFIED/FAILED).");

            // Allowed transitions: PENDING -> VERIFIED/FAILED
            if (caseEntity.Status != KycStatus.PENDING)
                throw new InvalidOperationException($"Unsupported transition from {caseEntity.Status}.");

            var oldStatus = caseEntity.Status;

            caseEntity.Status = target;
            caseEntity.ProviderRef = dto.ProviderRef ?? caseEntity.ProviderRef;

            // Update evidence refs if provided
            if (dto.EvidenceRefs is not null)
            {
                caseEntity.EvidenceRefsJson = JsonSerializer.Serialize(dto.EvidenceRefs);
            }

            // CheckedAt must be set on terminal states
            if (target == KycStatus.VERIFIED || target == KycStatus.FAILED)
            {
                caseEntity.CheckedAt = dto.CheckedAt ?? DateTime.UtcNow;
            }

            // Map DTO -> entity
            var entity = _mapper.Map<KycCase>(dto);

            var before = new { entity.Status, entity.ProviderRef, entity.CheckedAt };

            await _kycCases.UpdateAsync(caseEntity, ct);
            await _kycCases.SaveChangesAsync(ct);

            // Reflect on Customer aggregate (VERIFIED only)
            if (target == KycStatus.VERIFIED)
            {
                var customer = await _customers.GetByIdAsync(caseEntity.CustomerId, ct);
                if (customer is not null)
                {
                    customer.Status = CustomerStatus.VERIFIED;
                    await _customers.UpdateAsync(customer, ct);
                    await _customers.SaveChangesAsync(ct);
                }
            }

            // Publish KYC status changed event
            await _publish.Publish<KycStatusChanged>(new
            {
                KycCaseId = caseEntity.KycCaseId,
                caseEntity.CustomerId,
                OldStatus = oldStatus,
                NewStatus = caseEntity.Status,
                ProviderRef = caseEntity.ProviderRef,
                CheckedAtUtc = caseEntity.CheckedAt
            }, ct);

            var after = new { entity.Status, entity.ProviderRef, entity.CheckedAt };

            // Audit: KycStatusChanged (before/after JSON)
            await _audit.RecordAsync(new AuditEntryDto(
                EntityType: "KycCase",
                Action: AuditAction.KycStatusChanged,
                TargetEntityId: entity.KycCaseId,
                RelatedEntityId: entity.CustomerId,
                Actor: "KycCaseService",
                CorrelationId: _corr.Get(),
                BeforeSnapshot: before,
                AfterSnapshot: after,
                Source: "API"
            ), ct);


            // (Optional) Audit: status change
            // await _audit.RecordAsync(...)

            return caseEntity;
        }

        public Task<KycCase?> GetByIdAsync(Guid caseId, CancellationToken ct) =>
            _kycCases.GetByIdAsync(caseId, ct);

        public Task<IReadOnlyList<KycCase>> GetByCustomerAsync(Guid customerId, CancellationToken ct) =>
            _kycCases.GetByCustomerAsync(customerId, ct);

        private static KycStatus MapDtoStatus(KycStatusDto dto) => dto switch
        {
            KycStatusDto.PENDING => KycStatus.PENDING,
            KycStatusDto.VERIFIED => KycStatus.VERIFIED,
            KycStatusDto.FAILED => KycStatus.FAILED,
            _ => throw new ValidationException("Unsupported KYC status.")
        };
    }
}
