using System;
using System.Collections.Generic;
using FluentValidation;

namespace Core.DTOS.Validators
{
    public sealed class KycStatusUpdateRequestDtoValidator : AbstractValidator<KycStatusUpdateRequestDto>
    {
        public KycStatusUpdateRequestDtoValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required.");

            RuleFor(x => x.KycCaseId)
                .NotEmpty().WithMessage("KycCaseId is required.");

            // Ensure status is a valid enum value
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Status must be a valid KYC status.");

            When(x => !string.IsNullOrWhiteSpace(x.ProviderRef), () =>
            {
                RuleFor(x => x.ProviderRef!)
                    .MaximumLength(400).WithMessage("ProviderRef must not exceed 400 characters.");
            });

            When(x => x.EvidenceRefs is not null, () =>
            {
                RuleFor(x => x.EvidenceRefs!)
                    .Must(list => list.Count > 0).WithMessage("EvidenceRefs must contain at least one reference when provided.");

                RuleForEach(x => x.EvidenceRefs!)
                    .NotEmpty().WithMessage("Evidence reference must not be empty.")
                    .MaximumLength(400).WithMessage("Each evidence reference must not exceed 400 characters.");
            });

            When(x => x.CheckedAt.HasValue, () =>
            {
                RuleFor(x => x.CheckedAt.Value)
                    .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("CheckedAt must not be in the future.");
            });
        }
    }
}