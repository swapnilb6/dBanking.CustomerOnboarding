using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Messages
{
    public interface CustomerCreated
    {
        Guid CustomerId { get; }
        string FirstName { get; }
        string LastName { get; }
        string Email { get; } // consider hashing if PII concerns
        DateTime CreatedAtUtc { get; }
        string SourceSystem { get; }  // e.g., "CustomerApi"
        Guid CorrelationId { get; }   // ties a workflow together
    }

    // create interface for Customer update
    public interface CustomerUpdated
    {
        Guid CustomerId { get; }

        // Updated values (nullable to indicate fields that were not part of the update)
        string? FirstName { get; }
        string? LastName { get; }
        string? Email { get; } // consider hashing if PII concerns

        // Names of properties that changed in this update (empty if none provided)
        IReadOnlyList<string> UpdatedFields { get; }

        // Metadata
        DateTime UpdatedAtUtc { get; }
        string SourceSystem { get; }  // e.g., "CustomerApi"
        string? UpdatedBy { get; }    // user id or service that performed the update
        Guid CorrelationId { get; }   // ties a workflow together
    }

    public interface KycStatusChanged
    {
        Guid KycCaseId { get; }
        Guid CustomerId { get; }
        string KycStatus { get; }     // e.g., "Pending", "InProgress", "Approved", "Rejected"
        string? Reason { get; }       // rejection reason if any
        DateTime StatusChangedAtUtc { get; }
        string SourceSystem { get; }
        Guid CorrelationId { get; }
  
        Core.Entities.KycStatus OldStatus { get; }
        Core.Entities.KycStatus NewStatus { get; }
        DateTime? CheckedAtUtc { get; }
        string? ProviderRef { get; }

    }

}
