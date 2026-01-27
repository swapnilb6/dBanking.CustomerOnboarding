using FluentValidation;
using Core.DTOS;

namespace Core.DTOS.Validators
{
    public sealed class KycStatusUpdateRequestValidator : AbstractValidator<KycStatusUpdateRequestDto>
    {
        public KycStatusUpdateRequestValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.KycCaseId).NotEmpty();
            RuleFor(x => x.Status).IsInEnum();

            // Only VERIFIED/FAILED allowed via update (PENDING is initial state)
            RuleFor(x => x.Status)
                .Must(s => s == KycStatusDto.VERIFIED || s == KycStatusDto.FAILED)
                .WithMessage("Status update must be to VERIFIED or FAILED.");

            RuleForEach(x => x.EvidenceRefs!)
                .MaximumLength(256)
                .When(x => x.EvidenceRefs is not null);

            // CheckedAt required (or auto-set) for terminal states; allow null and fill in service
        }
    }
}
