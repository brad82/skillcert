using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Records;
using SkillCert.Domain.Users;

namespace SkillCert.Infrastructure.Persistence.Configurations;

internal sealed class TrainingRecordConfiguration : IEntityTypeConfiguration<TrainingRecord>
{
    public void Configure(EntityTypeBuilder<TrainingRecord> builder)
    {
        builder.ToTable("training_records");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.BlobPath).HasMaxLength(400);
        builder.Property(r => r.Sha256Hash).HasMaxLength(64).IsFixedLength();
        builder.Property(r => r.Trigger).HasConversion<string>().HasMaxLength(30);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompetencyList>().WithMany().HasForeignKey(r => r.CompetencyListId).OnDelete(DeleteBehavior.Restrict);

        // Spec §22: automatic archival is idempotent per user, list and completion date; manual archives are separate.
        builder.HasIndex(r => new { r.UserId, r.CompetencyListId, r.CompletionDate })
            .IsUnique()
            .HasFilter("trigger = 'ComplianceAchieved'")
            .HasDatabaseName("ux_training_records_automatic");
        builder.HasIndex(r => new { r.UserId, r.GeneratedAt });
    }
}

internal sealed class ComplianceCheckpointConfiguration : IEntityTypeConfiguration<ComplianceCheckpoint>
{
    public void Configure(EntityTypeBuilder<ComplianceCheckpoint> builder)
    {
        builder.ToTable("compliance_checkpoints");
        builder.HasKey(c => new { c.UserId, c.CompetencyListId });
        builder.Ignore(c => c.WouldBeNewlyCompliant);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CompetencyList>().WithMany().HasForeignKey(c => c.CompetencyListId).OnDelete(DeleteBehavior.Cascade);
    }
}
