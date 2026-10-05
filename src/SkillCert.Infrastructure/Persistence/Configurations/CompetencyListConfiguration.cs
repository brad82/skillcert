using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Lists;

namespace SkillCert.Infrastructure.Persistence.Configurations;

internal sealed class CompetencyListConfiguration : IEntityTypeConfiguration<CompetencyList>
{
    public void Configure(EntityTypeBuilder<CompetencyList> builder)
    {
        builder.ToTable("competency_lists");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Title).HasMaxLength(200);
        builder.Property(l => l.Description).HasMaxLength(2000);
        builder.Ignore(l => l.CompetencyIds);

        builder.HasMany(l => l.Nodes)
            .WithOne()
            .HasForeignKey(n => n.CompetencyListId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.Nodes).HasField("_nodes");
    }
}

/// <summary>
/// The tree's rules live in the CompetencyList aggregate; these constraints back them up in the database:
/// a parent must be a heading of the same list (composite FK onto (id, list, kind) with the child's
/// parent_kind pinned to 'Heading'), each node is exactly a heading or a competency leaf, and a
/// competency appears once per list.
/// </summary>
internal sealed class CompetencyListNodeConfiguration : IEntityTypeConfiguration<CompetencyListNode>
{
    private const string ParentKind = "ParentKind";

    public void Configure(EntityTypeBuilder<CompetencyListNode> builder)
    {
        builder.ToTable("competency_list_nodes", t =>
        {
            t.HasCheckConstraint("ck_competency_list_nodes_shape",
                "(kind = 'Heading' AND heading_title IS NOT NULL AND competency_id IS NULL) OR " +
                "(kind = 'Competency' AND competency_id IS NOT NULL AND heading_title IS NULL AND heading_code IS NULL)");
            t.HasCheckConstraint("ck_competency_list_nodes_parent_is_heading", "parent_kind = 'Heading'");
        });
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.HeadingCode).HasMaxLength(50);
        builder.Property(n => n.HeadingTitle).HasMaxLength(300);

        builder.Property<ListNodeKind>(ParentKind)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ListNodeKind.Heading)
            .HasSentinel(ListNodeKind.Heading);
        builder.HasAlternateKey(n => new { n.Id, n.CompetencyListId, n.Kind });
        builder.HasOne<CompetencyListNode>()
            .WithMany()
            .HasForeignKey(nameof(CompetencyListNode.ParentNodeId), nameof(CompetencyListNode.CompetencyListId), ParentKind)
            .HasPrincipalKey(nameof(CompetencyListNode.Id), nameof(CompetencyListNode.CompetencyListId), nameof(CompetencyListNode.Kind))
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.CompetencyListId, n.CompetencyId })
            .IsUnique()
            .HasFilter("competency_id IS NOT NULL");
        builder.HasIndex(n => new { n.CompetencyListId, n.ParentNodeId, n.SortOrder });

        builder.HasOne<Competency>()
            .WithMany()
            .HasForeignKey(n => n.CompetencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
