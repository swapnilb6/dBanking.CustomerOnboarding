// Infrastructure/Persistence/Configurations/AuditRecordEntityTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Entities;

namespace Infrastructure.Configurations;
public sealed class AuditRecordEntityTypeConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> e)
    {
        e.ToTable("AuditRecords");

        e.HasKey(x => x.AuditRecordId);

        e.Property(x => x.EntityType)
         .IsRequired();

        // Explicitly store enum as int (EF does this by default, but we make it explicit)
        e.Property(x => x.Action)
         .HasConversion<int>()
         .IsRequired();

        e.Property(x => x.TargetEntityId)
         .IsRequired(false);

        e.Property(x => x.RelatedEntityId)
         .IsRequired(false);

        e.Property(x => x.Actor)
         .HasMaxLength(256)
         .IsRequired();

        e.Property(x => x.CorrelationId)
         .IsRequired(false);

        // DateTimeOffset -> timestamptz
        e.Property(x => x.Timestamp)
         .IsRequired();

        // JSON as jsonb, nullable
        e.Property(x => x.BeforeJson)
         .HasColumnType("jsonb")
         .IsRequired(false);

        e.Property(x => x.AfterJson)
         .HasColumnType("jsonb")
         .IsRequired(false);

        e.Property(x => x.Source)
         .IsRequired(false);

        e.Property(x => x.Environment)
         .IsRequired(false);

        e.HasIndex(x => new { x.EntityType, x.TargetEntityId, x.Timestamp });
        e.HasIndex(x => x.CorrelationId);
        e.HasIndex(x => new { x.RelatedEntityId, x.Timestamp });
    }
}
