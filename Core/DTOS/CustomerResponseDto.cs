using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOS
{
    public sealed record CustomerResponseDto(
        Guid CustomerId,
        string FirstName,
        string LastName,
        DateOnly Dob,
        string Email,
        string Phone,
        CustomerStatusDto Status,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        IReadOnlyList<KycCaseSummaryDto>? KycCases = null
    );

}
