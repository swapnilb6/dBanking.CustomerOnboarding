using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Core.Entities
{
    public class Customer
    {
        [Key]
        public Guid CustomerId { get; set; } = Guid.NewGuid();

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        // DateOnly is supported by EF Core 8; ensure your provider maps to 'date'
        [Required]
        [Column(TypeName = "date")]
        public DateOnly Dob { get; set; }

        // These may be validated more strictly in DTOs/validators;
        // keep lightweight constraints at the entity level.
        [Required, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(32)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public CustomerStatus Status { get; set; } = CustomerStatus.PENDING_KYC;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Relationships

        [JsonIgnore]
        public List<KycCase> KycCases { get; set; } = new();
    }
}
