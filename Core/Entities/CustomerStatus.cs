using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Core.Entities
{
    public enum CustomerStatus
    {
        PENDING_KYC = 0,
        VERIFIED = 1,
        CLOSED = 2
    }
    public enum KycStatus
    {
        PENDING = 0,
        VERIFIED = 1,
        FAILED = 2
    }
    public enum AuditEntityType
    {
        Customer = 0,
        KycCase = 1
    }
    public enum AuditAction
    {

        Create = 1,
        Update = 2,
        KycStarted = 3,
        KycStatusChanged = 4

    }
}
