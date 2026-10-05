using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillCert.Domain.Groups;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Users;

namespace SkillCert.Infrastructure.Persistence.Configurations;

internal sealed class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("user_groups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Name).HasMaxLength(100);
        builder.HasIndex(g => g.Name).IsUnique();
        builder.Property(g => g.Description).HasMaxLength(1000);

        builder.HasMany(g => g.Members).WithOne().HasForeignKey(m => m.UserGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Members).HasField("_members");
        builder.HasMany(g => g.AssignedLists).WithOne().HasForeignKey(a => a.UserGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.AssignedLists).HasField("_assignedLists");
    }
}

internal sealed class UserGroupMembershipConfiguration : IEntityTypeConfiguration<UserGroupMembership>
{
    public void Configure(EntityTypeBuilder<UserGroupMembership> builder)
    {
        builder.ToTable("user_group_memberships");
        builder.HasKey(m => new { m.UserGroupId, m.UserId });
        builder.HasIndex(m => m.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class GroupListAssignmentConfiguration : IEntityTypeConfiguration<GroupListAssignment>
{
    public void Configure(EntityTypeBuilder<GroupListAssignment> builder)
    {
        builder.ToTable("group_list_assignments");
        builder.HasKey(a => new { a.UserGroupId, a.CompetencyListId });
        builder.HasOne<CompetencyList>().WithMany().HasForeignKey(a => a.CompetencyListId).OnDelete(DeleteBehavior.Restrict);
    }
}
