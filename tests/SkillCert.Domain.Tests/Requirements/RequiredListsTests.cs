using SkillCert.Domain.Groups;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Requirements;

namespace SkillCert.Domain.Tests.Requirements;

public sealed class RequiredListsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Jo = Guid.NewGuid();
    private static readonly Guid Shared = Guid.NewGuid();

    private static CompetencyList List(string title, params Guid[] competencies)
    {
        var list = new CompetencyList(title, null, Now);
        foreach (var competency in competencies)
        {
            list.AddCompetency(null, competency);
        }

        return list;
    }

    private static UserGroup Group(string name, Guid member, params CompetencyList[] lists)
    {
        var group = new UserGroup(name, null, Now);
        group.AddMember(member, Now);
        foreach (var list in lists)
        {
            group.AssignList(list.Id, Now);
        }

        return group;
    }

    [Fact]
    public void Required_lists_are_the_distinct_union_across_groups()
    {
        var afa = List("AFA Skills Record", Shared);
        var toboggan = List("Toboggan", Guid.NewGuid());
        var groups = new[] { Group("New Patroller", Jo, afa), Group("Senior Patroller", Jo, afa, toboggan) };

        var required = RequiredLists.For(Jo, groups, [afa, toboggan]);

        Assert.Equal(["AFA Skills Record", "Toboggan"], required.Select(l => l.Title));
    }

    [Fact]
    public void A_shared_competency_is_one_requirement()
    {
        var afa = List("AFA Skills Record", Shared, Guid.NewGuid());
        var refresher = List("Refresher", Shared);

        var competencies = RequiredLists.Competencies([afa, refresher]);

        Assert.Equal(2, competencies.Count);
    }

    [Fact]
    public void Groups_the_user_is_not_in_add_nothing()
    {
        var afa = List("AFA Skills Record", Shared);
        var someoneElse = Group("New Patroller", Guid.NewGuid(), afa);

        Assert.Empty(RequiredLists.For(Jo, [someoneElse], [afa]));
    }

    [Fact]
    public void Inactive_groups_and_inactive_lists_impose_nothing()
    {
        var afa = List("AFA Skills Record", Shared);
        var retired = List("Retired list", Guid.NewGuid());
        retired.Deactivate();
        var dormant = Group("Returning Patroller", Jo, afa);
        dormant.Deactivate();

        Assert.Empty(RequiredLists.For(Jo, [dormant, Group("Senior Patroller", Jo, retired)], [afa, retired]));
    }

    [Fact]
    public void Membership_and_assignment_are_idempotent()
    {
        var afa = List("AFA Skills Record", Shared);
        var group = Group("New Patroller", Jo, afa);

        group.AddMember(Jo, Now.AddDays(1));
        group.AssignList(afa.Id, Now.AddDays(1));

        Assert.Single(group.Members);
        Assert.Single(group.AssignedLists);
    }
}
