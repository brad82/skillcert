using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Users;

namespace SkillCert.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.ExternalSubjectId).HasMaxLength(256);
        builder.HasIndex(u => u.ExternalSubjectId).IsUnique();
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.Email).HasMaxLength(256);

        builder.HasMany(u => u.Classifications)
            .WithOne()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.Classifications).HasField("_classifications");
    }
}

internal sealed class UserReviewerClassificationConfiguration : IEntityTypeConfiguration<UserReviewerClassification>
{
    public void Configure(EntityTypeBuilder<UserReviewerClassification> builder)
    {
        builder.ToTable("user_reviewer_classifications");
        builder.HasKey(c => new { c.UserId, c.ReviewerClassificationId });
        builder.HasOne<ReviewerClassification>()
            .WithMany()
            .HasForeignKey(c => c.ReviewerClassificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReviewerClassificationConfiguration : IEntityTypeConfiguration<ReviewerClassification>
{
    public void Configure(EntityTypeBuilder<ReviewerClassification> builder)
    {
        builder.ToTable("reviewer_classifications");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Code).HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.Property(c => c.AffirmationPolicy).HasConversion<string>().HasMaxLength(50);

        // Seeded reference data (spec §7): Instructor is Automatic, Supervisor needs reviewer confirmation.
        builder.HasData(
            new ReviewerClassification(
                ReviewerClassification.InstructorId, "Instructor", "Instructor", AffirmationPolicy.Automatic),
            new ReviewerClassification(
                ReviewerClassification.SupervisorId, "Supervisor", "Supervisor", AffirmationPolicy.ReviewerConfirmation));
    }
}
