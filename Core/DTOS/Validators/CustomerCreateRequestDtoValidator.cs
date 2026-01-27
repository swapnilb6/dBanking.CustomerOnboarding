using System;
using FluentValidation;

namespace Core.DTOS.Validators
{
    public sealed class CustomerCreateRequestDtoValidator : AbstractValidator<CustomerCreateRequestDto>
    {
        public CustomerCreateRequestDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

            RuleFor(x => x.Dob)
                .Must(BeAValidDob).WithMessage("Date of birth must be a past date and customer must be at least 18 years old.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone number is required.")
                .MaximumLength(32).WithMessage("Phone must not exceed 32 characters.")
                .Matches(@"^\+?[0-9]{7,15}$").WithMessage("Phone must be a valid phone number (digits, optional leading '+', 7-15 digits).");

            When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey), () =>
            {
                RuleFor(x => x.IdempotencyKey!)
                    .MaximumLength(200).WithMessage("Idempotency key must not exceed 200 characters.");
            });
        }

        private static bool BeAValidDob(DateOnly dob)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dob > today) return false;

            // Ensure at least 18 years old
            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            return age >= 18;
        }
    }
}