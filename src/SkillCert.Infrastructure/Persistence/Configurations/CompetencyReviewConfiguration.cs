using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Users;

namespace SkillCert.Infrastructure.Persistence.Configurations;

internal sealed class CompetencyReviewConfiguration : IEntityTypeConfiguration<CompetencyReview>
{
    public void Configure(EntityTypeBuilder<CompetencyReview> builder)
    {
        builder.ToTable("competency_reviews", t =>
        {
            t.HasCheckConstraint("ck_competency_reviews_confirmed_fields",
                "(confirmation_status = 'Confirmed') = (confirmed_at IS NOT NULL AND confirmed_by_user_id IS NOT NULL)");
            t.HasCheckConstraint("ck_competency_reviews_rejected_fields",
                "(confirmation_status = 'Rejected') = (rejected_at IS NOT NULL AND rejected_by_user_id IS NOT NULL AND rejection_reason IS NOT NULL)");
        });
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Ignore(r => r.IsAccepted);
        builder.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ConfirmationStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ReviewerName).HasMaxLength(200);
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);

        builder.HasOne<Competency>().WithMany().HasForeignKey(r => r.CompetencyId).OnDelete(DeleteBehavior.Restrict);
        // The assessed revision must belong to the reviewed competency.
        builder.HasOne<CompetencyRevision>()
            .WithMany()
            .HasForeignKey(r => new { r.CompetencyRevisionId, r.CompetencyId })
            .HasPrincipalKey(r => new { r.Id, r.CompetencyId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.CandidateUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ReviewerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RejectedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReviewerClassification>().WithMany().HasForeignKey(r => r.ReviewerClassificationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReviewSignature>().WithMany().HasForeignKey(r => r.ReviewSignatureId).OnDelete(DeleteBehavior.Restrict);

        // Currency reads a user's reviews per competency, newest first.
        builder.HasIndex(r => new { r.CandidateUserId, r.CompetencyId, r.ReviewedAt });
        // The approval queue: a reviewer's pending claims.
        builder.HasIndex(r => new { r.ReviewerUserId, r.ReviewedAt })
            .HasFilter("confirmation_status = 'Pending'")
            .HasDatabaseName("ix_competency_reviews_pending_by_reviewer");
    }
}

internal sealed class ReviewSignatureConfiguration : IEntityTypeConfiguration<ReviewSignature>
{
    public void Configure(EntityTypeBuilder<ReviewSignature> builder)
    {
        builder.ToTable("review_signatures");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.ContentType).HasMaxLength(100);
    }
}
