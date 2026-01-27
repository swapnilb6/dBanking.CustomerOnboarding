using FluentValidation;
using Core.DTOS;

namespace Core.DTOS.Validators
{
    public sealed class KycCaseCreateValidator : AbstractValidator<KycCaseCreateRequestDto>
    {
        public KycCaseCreateValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.EvidenceRefs).NotEmpty().WithMessage("At least one evidence reference is required.");
            RuleForEach(x => x.EvidenceRefs).NotEmpty().MaximumLength(256);
            RuleFor(x => x.ConsentText).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.AcceptedAt)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("AcceptedAt cannot be in the future.");
        }
    }
}
