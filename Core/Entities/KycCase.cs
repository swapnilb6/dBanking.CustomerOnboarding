using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Core.Entities
{
    public class KycCase
    {
        [Key]
        public Guid KycCaseId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CustomerId { get; set; }

        // Navigation back to Customer
        public Customer Customer { get; set; } = default!;

        [Required]
        public KycStatus Status { get; set; } = KycStatus.PENDING;
        public string? ProviderRef { get; set; }

        // Persisted as jsonb in DB
        public string? EvidenceRefsJson { get; set; }

        // Computed view (not mapped)
        [NotMapped]
        public IReadOnlyList<string> EvidenceRefs =>
            string.IsNullOrWhiteSpace(EvidenceRefsJson)
                ? Array.Empty<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(EvidenceRefsJson)!;



        /// <summary>
        /// Consent text displayed to the customer (versioned in provider/backend).
        /// </summary>
        [Required]
        public string ConsentText { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp when consent was accepted.
        /// </summary>
        [Required]
        public DateTime AcceptedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CheckedAt { get; set; }
    }

}
