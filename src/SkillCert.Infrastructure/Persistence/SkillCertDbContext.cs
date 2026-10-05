using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillCert.Domain.Audit;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Groups;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Records;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Users;
using SkillCert.Infrastructure.Identity;

namespace SkillCert.Infrastructure.Persistence;

public sealed class SkillCertDbContext(DbContextOptions<SkillCertDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<User> DomainUsers => Set<User>();

    public DbSet<ReviewerClassification> ReviewerClassifications => Set<ReviewerClassification>();

    public DbSet<Competency> Competencies => Set<Competency>();

    public DbSet<CompetencyList> CompetencyLists => Set<CompetencyList>();

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    public DbSet<CompetencyReview> CompetencyReviews => Set<CompetencyReview>();

    public DbSet<ReviewSignature> ReviewSignatures => Set<ReviewSignature>();

    public DbSet<TrainingRecord> TrainingRecords => Set<TrainingRecord>();

    public DbSet<ComplianceCheckpoint> ComplianceCheckpoints => Set<ComplianceCheckpoint>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>ASP.NET data-protection keys (they encrypt the auth cookie), kept here so container restarts don't sign everyone out.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(SkillCertDbContext).Assembly);
    }
}
