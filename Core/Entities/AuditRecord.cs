using System.ComponentModel.DataAnnotations;
namespace Core.Entities
{
    public sealed class AuditRecord
    {

        public Guid AuditRecordId { get; init; } = Guid.NewGuid();
        public string EntityType { get; init; } = default!;
        public AuditAction Action { get; init; }   // <-- enum, not string
        public Guid? TargetEntityId { get; init; }
        public Guid? RelatedEntityId { get; init; }
        public string Actor { get; init; } = default!;
        public string? CorrelationId { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public string? BeforeJson { get; init; }
        public string? AfterJson { get; init; }
        public string? Source { get; init; }
        public string? Environment { get; init; }
        // "dev", "qa", "prod"
    }
}
