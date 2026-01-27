// Infrastructure/Persistence/Configurations/AuditRecordEntityTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Entities;

namespace Infrastructure.Configurations;
public sealed class AuditRecordEntityTypeConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> b)
    {
        b.ToTable("AuditRecords", schema: "public");


        // Map PK property to existing column name "AuditId"
        b.HasKey(x => x.AuditRecordId);
        b.Property(x => x.AuditRecordId).HasColumnName("AuditId");

        b.Property(x => x.EntityType).HasColumnName("EntityType").HasMaxLength(100);

        // Store enum as int in the DB (matches existing integer column)
        b.Property(x => x.Action)
            .HasColumnName("Action")
            .HasConversion<int>();   // <-- important

        b.Property(x => x.Actor).HasColumnName("Actor").HasMaxLength(200);
        b.Property(x => x.CorrelationId).HasColumnName("CorrelationId").HasMaxLength(100);
        b.Property(x => x.Timestamp).HasColumnName("Timestamp");
        b.Property(x => x.BeforeJson).HasColumnName("BeforeJson").HasColumnType("jsonb");
        b.Property(x => x.AfterJson).HasColumnName("AfterJson").HasColumnType("jsonb");

        b.Property(x => x.TargetEntityId).HasColumnName("TargetEntityId");
        b.Property(x => x.RelatedEntityId).HasColumnName("RelatedEntityId");
        b.Property(x => x.Source).HasColumnName("Source").HasMaxLength(50);
        b.Property(x => x.Environment).HasColumnName("Environment").HasMaxLength(20);


        // Indexes (optional)
        b.HasIndex(x => new { x.EntityType, x.TargetEntityId });
        b.HasIndex(x => x.Timestamp);
        b.HasIndex(x => x.CorrelationId);
        b.HasIndex(x => x.Action);

    }
}
