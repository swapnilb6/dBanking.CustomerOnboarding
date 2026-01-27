using System;
using FluentValidation;

namespace Core.DTOS.Validators
{
    public sealed class CustomerUpdateRequestDtoValidator : AbstractValidator<CustomerUpdateRequestDto>
    {
        public CustomerUpdateRequestDtoValidator()
        {
            When(x => x.FirstName is not null, () =>
            {
                RuleFor(x => x.FirstName!)
                    .NotEmpty().WithMessage("First name must not be empty when provided.")
                    .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");
            });

            When(x => x.LastName is not null, () =>
            {
                RuleFor(x => x.LastName!)
                    .NotEmpty().WithMessage("Last name must not be empty when provided.")
                    .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");
            });

            When(x => x.Dob.HasValue, () =>
            {
                RuleFor(x => x.Dob)
                    .Must(BeAValidDob).WithMessage("Date of birth must be a past date and customer must be at least 18 years old.");
            });
        }

        private static bool BeAValidDob(DateOnly? dob)
        {
            if (!dob.HasValue) return true;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var d = dob.Value;
            if (d > today) return false;

            var age = today.Year - d.Year;
            if (d > today.AddYears(-age)) age--;
            return age >= 18;
        }
    }
}