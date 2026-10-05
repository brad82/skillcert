using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;

namespace SkillCert.Infrastructure.Persistence.Configurations;

internal sealed class CompetencyConfiguration : IEntityTypeConfiguration<Competency>
{
    public void Configure(EntityTypeBuilder<Competency> builder)
    {
        builder.ToTable("competencies");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Code).HasMaxLength(50);
        builder.Property(c => c.NormalizedCode).HasMaxLength(50);
        // Spec §26: existing codes, active or not, are conflicts; this index is the concurrency guard.
        builder.HasIndex(c => c.NormalizedCode).IsUnique();
        builder.Ignore(c => c.CurrentRevision);

        builder.HasMany(c => c.Revisions)
            .WithOne()
            .HasForeignKey(r => r.CompetencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Revisions).HasField("_revisions");
    }
}

internal sealed class CompetencyRevisionConfiguration : IEntityTypeConfiguration<CompetencyRevision>
{
    public void Configure(EntityTypeBuilder<CompetencyRevision> builder)
    {
        builder.ToTable("competency_revisions", t =>
            t.HasCheckConstraint("ck_competency_revisions_recertification_days_positive",
                "recertification_days IS NULL OR recertification_days > 0"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasIndex(r => new { r.CompetencyId, r.RevisionNumber }).IsUnique();
        // Lets a review's (revision, competency) pair be checked by one composite foreign key.
        builder.HasAlternateKey(r => new { r.Id, r.CompetencyId });
        builder.Property(r => r.Title).HasMaxLength(300);
        builder.Property(r => r.ShortTitle).HasMaxLength(300);
        builder.Property(r => r.Description).HasMaxLength(4000);

        builder.OwnsMany(r => r.Resources, resources =>
        {
            resources.ToJson();
            resources.Property(x => x.Title).HasMaxLength(200);
            resources.Property(x => x.Url).HasConversion(u => u.ToString(), s => new Uri(s));
            resources.Property(x => x.Type).HasConversion<string>();
        });
        builder.Navigation(r => r.Resources).HasField("_resources");

        builder.HasMany(r => r.PermittedClassifications)
            .WithOne()
            .HasForeignKey(p => p.CompetencyRevisionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.PermittedClassifications).HasField("_permittedClassifications");
    }
}

internal sealed class RevisionPermittedClassificationConfiguration : IEntityTypeConfiguration<RevisionPermittedClassification>
{
    public void Configure(EntityTypeBuilder<RevisionPermittedClassification> builder)
    {
        builder.ToTable("revision_permitted_classifications");
        builder.HasKey(p => new { p.CompetencyRevisionId, p.ReviewerClassificationId });
        builder.HasOne<ReviewerClassification>()
            .WithMany()
            .HasForeignKey(p => p.ReviewerClassificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
