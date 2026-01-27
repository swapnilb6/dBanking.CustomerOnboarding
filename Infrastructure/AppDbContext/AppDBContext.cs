using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DbContext
{
    /// <summary>
    /// EF Core DbContext for Customer Onboarding bounded context, backed by SQL Server.
    /// </summary>
    public class AppDBContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<KycCase> KycCases => Set<KycCase>();
        public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // --- Entity mappings (SQL Server) ---
            b.Entity<Customer>(e =>
            {
                e.HasKey(x => x.CustomerId);
                e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
                e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
                e.Property(x => x.Dob).HasColumnType("date").IsRequired();
                e.Property(x => x.Email).HasMaxLength(256).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(32).IsRequired();
                e.Property(x => x.Status).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();

                e.HasIndex(x => x.Email).IsUnique();
                e.HasIndex(x => x.Phone);
                e.HasIndex(x => new { x.FirstName, x.LastName, x.Dob });

                e.HasMany(x => x.KycCases)
                 .WithOne(x => x.Customer)
                 .HasForeignKey(x => x.CustomerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<KycCase>(e =>
            {
                e.HasKey(x => x.KycCaseId);
                e.Property(x => x.Status).IsRequired();
                e.Property(x => x.ConsentText).HasMaxLength(4000).IsRequired();
                e.Property(x => x.AcceptedAt).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();

                // SQL Server JSON → NVARCHAR(MAX)
                e.Property(x => x.EvidenceRefsJson).HasColumnType("nvarchar(max)");

                e.HasIndex(x => new { x.CustomerId, x.Status });
            });

            b.Entity<AuditRecord>(e =>
            {
                e.HasKey(x => x.AuditRecordId);
                e.Property(x => x.EntityType).IsRequired();
                e.Property(x => x.TargetEntityId).IsRequired();
                e.Property(x => x.Action).IsRequired();
                e.Property(x => x.Actor).HasMaxLength(256).IsRequired();
                e.Property(x => x.Timestamp).IsRequired();
                e.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
                e.Property(x => x.AfterJson).HasColumnType("nvarchar(max)").IsRequired();

                e.HasIndex(x => new { x.EntityType, x.TargetEntityId, x.Timestamp });
            });

            // --- Seed mock data ---
            Seed(b);
        }

        private static void Seed(ModelBuilder b)
        {
            // Fixed IDs for stable seeding
            var cust1Id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
            var cust2Id = Guid.Parse("11112222-3333-4444-5555-666677778888");
            var cust3Id = Guid.Parse("99998888-7777-6666-5555-444433332222");

            var kyc1Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffff0001");
            var kyc2Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffff0002");

            var audit1Id = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00000001");
            var audit2Id = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00000002");
            var audit3Id = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00000003");

            // Customers
            b.Entity<Customer>().HasData(
                new Customer
                {
                    CustomerId = cust1Id,
                    FirstName = "Rahul",
                    LastName = "Sharma",
                    Dob = new DateOnly(1994, 4, 11),
                    Email = "rahul.sharma@example.com",
                    Phone = "+91-9876543210",
                    Status = CustomerStatus.PENDING_KYC,
                    CreatedAt = new DateTime(2025, 12, 1, 10, 30, 00, DateTimeKind.Utc),
                    UpdatedAt = null
                },
                new Customer
                {
                    CustomerId = cust2Id,
                    FirstName = "Neha",
                    LastName = "Kulkarni",
                    Dob = new DateOnly(1992, 6, 25),
                    Email = "neha.kulkarni@example.com",
                    Phone = "+91-9822001122",
                    Status = CustomerStatus.VERIFIED,
                    CreatedAt = new DateTime(2025, 11, 15, 08, 00, 00, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 11, 20, 09, 15, 00, DateTimeKind.Utc)
                },
                new Customer
                {
                    CustomerId = cust3Id,
                    FirstName = "Amit",
                    LastName = "Patil",
                    Dob = new DateOnly(1988, 1, 3),
                    Email = "amit.patil@example.com",
                    Phone = "+91-9812345678",
                    Status = CustomerStatus.CLOSED,
                    CreatedAt = new DateTime(2025, 10, 10, 11, 45, 00, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 12, 5, 14, 00, 00, DateTimeKind.Utc)
                }
            );

            // KYC Cases (FK → Customers)
            b.Entity<KycCase>().HasData(
                new KycCase
                {
                    KycCaseId = kyc1Id,
                    CustomerId = cust1Id,
                    Status = KycStatus.PENDING,
                    ProviderRef = null,
                    EvidenceRefsJson = """["doc-POI-123","doc-POA-456"]""",
                    ConsentText = "I consent to eKYC verification for account onboarding.",
                    AcceptedAt = new DateTime(2025, 12, 1, 10, 35, 00, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2025, 12, 1, 10, 36, 00, DateTimeKind.Utc),
                    CheckedAt = null
                },
                new KycCase
                {
                    KycCaseId = kyc2Id,
                    CustomerId = cust2Id,
                    Status = KycStatus.VERIFIED,
                    ProviderRef = "kyc-prov-789",
                    EvidenceRefsJson = """["doc-POI-777","doc-POA-888"]""",
                    ConsentText = "I consent to eKYC verification for account onboarding.",
                    AcceptedAt = new DateTime(2025, 11, 15, 08, 05, 00, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2025, 11, 15, 08, 06, 00, DateTimeKind.Utc),
                    CheckedAt = new DateTime(2025, 11, 15, 09, 10, 00, DateTimeKind.Utc)
                }
            );

            // Audit Records
            b.Entity<AuditRecord>().HasData(
                new AuditRecord
                {
                    AuditRecordId = audit1Id,
                    EntityType = "Customer",
                    TargetEntityId = cust1Id,
                    Action = AuditAction.Create,
                    Actor = "sub:rahul@aad",
                    Timestamp = new DateTime(2025, 12, 1, 10, 36, 30, DateTimeKind.Utc),
                    BeforeJson = null,
                    AfterJson = """{"customerId":"00112233-4455-6677-8899-aabbccddeeff","status":"PENDING_KYC"}""",
                    CorrelationId = "corr-rahul-001"
                },
                new AuditRecord
                {
                    AuditRecordId = audit2Id,
                    EntityType = "KycCase",
                    TargetEntityId = kyc1Id,
                    Action = AuditAction.KycStarted,
                    Actor = "sub:rahul@aad",
                    Timestamp = new DateTime(2025, 12, 1, 10, 36, 50, DateTimeKind.Utc),
                    BeforeJson = null,
                    AfterJson = """{"kycCaseId":"aaaaaaaa-bbbb-cccc-dddd-eeeeffff0001","status":"PENDING"}""",
                    CorrelationId = "corr-rahul-001"
                },
                new AuditRecord
                {
                    AuditRecordId = audit3Id,
                    EntityType = "Customer",
                    TargetEntityId = cust3Id,
                    Action = AuditAction.Update,
                    Actor = "system:closure",
                    Timestamp = new DateTime(2025, 12, 5, 14, 00, 20, DateTimeKind.Utc),
                    BeforeJson = """{"customerId":"99998888-7777-6666-5555-444433332222","status":"VERIFIED"}""",
                    AfterJson = """{"customerId":"99998888-7777-6666-5555-444433332222","status":"CLOSED"}""",
                    CorrelationId = "corr-closure-amit-01"
                }
            );
        }
    }
}


