using System;
using System.Linq;
using FluentValidation;

namespace Core.DTOS.Validators
{
    public sealed class KycCaseCreateRequestDtoValidator : AbstractValidator<KycCaseCreateRequestDto>
    {
        public KycCaseCreateRequestDtoValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required.");

            RuleFor(x => x.EvidenceRefs)
                .NotNull().WithMessage("EvidenceRefs is required.")
                .Must(list => list is { Count: > 0 }).WithMessage("At least one evidence reference is required.")
                .ForEach(refRule =>
                {
                    refRule.NotEmpty().WithMessage("Evidence reference must not be empty.")
                           .MaximumLength(400).WithMessage("Each evidence reference must not exceed 400 characters.");
                });

            RuleFor(x => x.ConsentText)
                .NotEmpty().WithMessage("Consent text is required.")
                .MaximumLength(4000).WithMessage("Consent text must not exceed 4000 characters.");

            RuleFor(x => x.AcceptedAt)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("AcceptedAt must not be in the future.");

            When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey), () =>
            {
                RuleFor(x => x.IdempotencyKey!)
                    .MaximumLength(200).WithMessage("Idempotency key must not exceed 200 characters.");
            });
        }
    }
}