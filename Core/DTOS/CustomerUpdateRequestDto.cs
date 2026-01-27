using System.ComponentModel.DataAnnotations;

namespace Core.DTOS
{
    public sealed record CustomerUpdateRequestDto(
        [property: StringLength(100)] string? FirstName = null,
        [property: StringLength(100)] string? LastName = null,
        DateOnly? Dob = null
    );

}
