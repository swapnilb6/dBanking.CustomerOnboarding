using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOS
{
    public enum CustomerStatusDto
    {
        PENDING_KYC = 0,
        VERIFIED = 1,
        CLOSED = 2
    }

    public enum KycStatusDto
    {
        PENDING = 0,
        VERIFIED = 1,
        FAILED = 2
    }

    public enum AuditEntityTypeDto
    {
        Customer = 0,
        KycCase = 1
    }

    public enum AuditActionDto
    {
        CREATE = 0,
        UPDATE = 1
    }

}
