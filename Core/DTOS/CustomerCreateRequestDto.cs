using System.ComponentModel.DataAnnotations;

namespace Core.DTOS
{
    public sealed record CustomerCreateRequestDto(
        [property: Required, StringLength(100)] string FirstName,
        [property: Required, StringLength(100)] string LastName,
        [property: Required] DateOnly Dob,
        [property: Required, EmailAddress, StringLength(256)] string Email,
        [property: Required, Phone, StringLength(32)] string Phone,
        string? IdempotencyKey = null
    );

}
