using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Core.DTOS
{
    public sealed record KycCaseCreateRequestDto(
        [property: Required] Guid CustomerId,
        [property: Required, MinLength(1)] List<string> EvidenceRefs,
        [property: Required, StringLength(4000)] string ConsentText,
        [property: Required] DateTime AcceptedAt,
        string? IdempotencyKey = null
    );

}
