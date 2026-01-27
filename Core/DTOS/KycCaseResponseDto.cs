using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOS
{
    public sealed record KycCaseResponseDto(
        Guid KycCaseId,
        Guid CustomerId,
        KycStatusDto Status,
        string? ProviderRef,
        IReadOnlyList<string> EvidenceRefs,
        string ConsentText,
        DateTime AcceptedAt,
        DateTime CreatedAt,
        DateTime? CheckedAt
    );

}
