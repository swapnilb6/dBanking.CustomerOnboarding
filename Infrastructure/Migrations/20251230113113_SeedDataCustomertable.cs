using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace dBanking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataCustomertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditRecords",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRecords", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dob = table.Column<DateOnly>(type: "date", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "KycCases",
                columns: table => new
                {
                    KycCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProviderRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceRefsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsentText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycCases", x => x.KycCaseId);
                    table.ForeignKey(
                        name: "FK_KycCases_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AuditRecords",
                columns: new[] { "AuditId", "Action", "Actor", "AfterJson", "BeforeJson", "CorrelationId", "EntityType", "TargetEntityId", "Timestamp" },
                values: new object[,]
                {
                    { new Guid("aaaabbbb-cccc-dddd-eeee-ffff00000001"), 0, "sub:rahul@aad", "{\"customerId\":\"00112233-4455-6677-8899-aabbccddeeff\",\"status\":\"PENDING_KYC\"}", null, "corr-rahul-001", 0, new Guid("00112233-4455-6677-8899-aabbccddeeff"), new DateTime(2025, 12, 1, 10, 36, 30, 0, DateTimeKind.Utc) },
                    { new Guid("aaaabbbb-cccc-dddd-eeee-ffff00000002"), 0, "sub:rahul@aad", "{\"kycCaseId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeffff0001\",\"status\":\"PENDING\"}", null, "corr-rahul-001", 1, new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeffff0001"), new DateTime(2025, 12, 1, 10, 36, 50, 0, DateTimeKind.Utc) },
                    { new Guid("aaaabbbb-cccc-dddd-eeee-ffff00000003"), 1, "system:closure", "{\"customerId\":\"99998888-7777-6666-5555-444433332222\",\"status\":\"CLOSED\"}", "{\"customerId\":\"99998888-7777-6666-5555-444433332222\",\"status\":\"VERIFIED\"}", "corr-closure-amit-01", 0, new Guid("99998888-7777-6666-5555-444433332222"), new DateTime(2025, 12, 5, 14, 0, 20, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "CustomerId", "CreatedAt", "Dob", "Email", "FirstName", "LastName", "Phone", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00112233-4455-6677-8899-aabbccddeeff"), new DateTime(2025, 12, 1, 10, 30, 0, 0, DateTimeKind.Utc), new DateOnly(1994, 4, 11), "rahul.sharma@example.com", "Rahul", "Sharma", "+91-9876543210", 0, null },
                    { new Guid("11112222-3333-4444-5555-666677778888"), new DateTime(2025, 11, 15, 8, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1992, 6, 25), "neha.kulkarni@example.com", "Neha", "Kulkarni", "+91-9822001122", 1, new DateTime(2025, 11, 20, 9, 15, 0, 0, DateTimeKind.Utc) },
                    { new Guid("99998888-7777-6666-5555-444433332222"), new DateTime(2025, 10, 10, 11, 45, 0, 0, DateTimeKind.Utc), new DateOnly(1988, 1, 3), "amit.patil@example.com", "Amit", "Patil", "+91-9812345678", 2, new DateTime(2025, 12, 5, 14, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "KycCases",
                columns: new[] { "KycCaseId", "AcceptedAt", "CheckedAt", "ConsentText", "CreatedAt", "CustomerId", "EvidenceRefsJson", "ProviderRef", "Status" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeffff0001"), new DateTime(2025, 12, 1, 10, 35, 0, 0, DateTimeKind.Utc), null, "I consent to eKYC verification for account onboarding.", new DateTime(2025, 12, 1, 10, 36, 0, 0, DateTimeKind.Utc), new Guid("00112233-4455-6677-8899-aabbccddeeff"), "[\"doc-POI-123\",\"doc-POA-456\"]", null, 0 },
                    { new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeffff0002"), new DateTime(2025, 11, 15, 8, 5, 0, 0, DateTimeKind.Utc), new DateTime(2025, 11, 15, 9, 10, 0, 0, DateTimeKind.Utc), "I consent to eKYC verification for account onboarding.", new DateTime(2025, 11, 15, 8, 6, 0, 0, DateTimeKind.Utc), new Guid("11112222-3333-4444-5555-666677778888"), "[\"doc-POI-777\",\"doc-POA-888\"]", "kyc-prov-789", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_EntityType_TargetEntityId_Timestamp",
                table: "AuditRecords",
                columns: new[] { "EntityType", "TargetEntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FirstName_LastName_Dob",
                table: "Customers",
                columns: new[] { "FirstName", "LastName", "Dob" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_KycCases_CustomerId_Status",
                table: "KycCases",
                columns: new[] { "CustomerId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditRecords");

            migrationBuilder.DropTable(
                name: "KycCases");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
