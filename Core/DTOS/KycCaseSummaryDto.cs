using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOS
{
    public sealed record KycCaseSummaryDto(
        Guid KycCaseId,
        KycStatusDto Status,
        string? ProviderRef,
        DateTime CreatedAt,
        DateTime? CheckedAt
    );

}
