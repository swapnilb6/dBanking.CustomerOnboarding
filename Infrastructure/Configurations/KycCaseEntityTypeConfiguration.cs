using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Configurations
{
    public sealed class KycCaseEntityTypeConfiguration : IEntityTypeConfiguration<KycCase>
    {
        public void Configure(EntityTypeBuilder<KycCase> b)
        {
            b.ToTable("KycCases", "public"); // match your actual name/case

            b.HasKey(x => x.KycCaseId);
            // Persisted column
            b.Property(x => x.EvidenceRefsJson)
             .HasColumnName("EvidenceRefs")
             .HasColumnType("jsonb");

            // THIS FIXES YOUR ERROR
            b.Ignore(x => x.EvidenceRefs);

            // other properties, enums, timestamps, etc.
        }
    }

}
