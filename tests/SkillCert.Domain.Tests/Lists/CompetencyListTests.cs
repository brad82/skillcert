using SkillCert.Domain.Lists;

namespace SkillCert.Domain.Tests.Lists;

public sealed class CompetencyListTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Cpr1 = Guid.NewGuid();
    private static readonly Guid Cpr2 = Guid.NewGuid();
    private static readonly Guid Aed = Guid.NewGuid();

    private static (CompetencyList List, CompetencyListNode Bls, CompetencyListNode Cpr) AfaFragment()
    {
        var list = new CompetencyList("AFA Skills Record", null, Now);
        var bls = list.AddHeading(null, "4", "Basic Life Support");
        var cpr = list.AddHeading(bls.Id, "4.3", "Cardiopulmonary resuscitation (CPR)");
        list.AddCompetency(cpr.Id, Cpr1);
        list.AddCompetency(cpr.Id, Cpr2);
        return (list, bls, cpr);
    }

    [Fact]
    public void Walk_returns_the_tree_depth_first_in_explicit_order()
    {
        var (list, bls, _) = AfaFragment();
        var aedHeading = list.AddHeading(bls.Id, "4.4", "Automatic external defibrillation (AED)");
        list.AddCompetency(aedHeading.Id, Aed);

        var order = list.Walk().Select(n => n.HeadingCode ?? n.CompetencyId.ToString()).ToList();

        Assert.Equal(["4", "4.3", Cpr1.ToString(), Cpr2.ToString(), "4.4", Aed.ToString()], order);
        Assert.Equal([Cpr1, Cpr2, Aed], list.CompetencyIds);
    }

    [Fact]
    public void Inserting_at_an_index_reorders_siblings_contiguously()
    {
        var (list, _, cpr) = AfaFragment();

        list.AddCompetency(cpr.Id, Aed, index: 0);

        Assert.Equal([Aed, Cpr1, Cpr2], list.ChildrenOf(cpr.Id).Select(n => n.CompetencyId!.Value));
        Assert.Equal([0, 1, 2], list.ChildrenOf(cpr.Id).Select(n => n.SortOrder));
    }

    [Fact]
    public void A_competency_can_appear_only_once_per_list()
    {
        var (list, bls, _) = AfaFragment();

        var error = Assert.Throws<DomainRuleException>(() => list.AddCompetency(bls.Id, Cpr1));

        Assert.Equal(CompetencyList.DuplicateCompetency, error.Code);
    }

    [Fact]
    public void Competency_leaves_cannot_have_children()
    {
        var (list, _, cpr) = AfaFragment();
        var leaf = list.ChildrenOf(cpr.Id)[0];

        var error = Assert.Throws<DomainRuleException>(() => list.AddHeading(leaf.Id, null, "Nested"));

        Assert.Equal(CompetencyList.InvalidParent, error.Code);
    }

    [Fact]
    public void A_parent_must_be_in_the_same_list()
    {
        var (list, _, _) = AfaFragment();
        var (other, otherBls, _) = AfaFragment();

        var error = Assert.Throws<DomainRuleException>(() => list.AddCompetency(otherBls.Id, Aed));

        Assert.Equal(CompetencyList.InvalidParent, error.Code);
        Assert.NotSame(list, other);
    }

    [Fact]
    public void A_heading_cannot_move_inside_its_own_subtree()
    {
        var (list, bls, cpr) = AfaFragment();

        Assert.Equal(CompetencyList.Cycle, Assert.Throws<DomainRuleException>(() => list.Move(bls.Id, cpr.Id, 0)).Code);
        Assert.Equal(CompetencyList.Cycle, Assert.Throws<DomainRuleException>(() => list.Move(bls.Id, bls.Id, 0)).Code);
    }

    [Fact]
    public void Moving_a_node_renumbers_both_the_old_and_new_siblings()
    {
        var (list, bls, cpr) = AfaFragment();
        var first = list.ChildrenOf(cpr.Id)[0];

        list.Move(first.Id, bls.Id, index: 0);

        Assert.Equal([0], list.ChildrenOf(cpr.Id).Select(n => n.SortOrder));
        Assert.Equal([first.Id, cpr.Id], list.ChildrenOf(bls.Id).Select(n => n.Id));
        Assert.Equal([0, 1], list.ChildrenOf(bls.Id).Select(n => n.SortOrder));
    }

    [Fact]
    public void Removing_a_heading_removes_its_subtree_and_frees_its_competencies()
    {
        var (list, bls, cpr) = AfaFragment();

        list.Remove(cpr.Id);

        Assert.Equal([bls.Id], list.Nodes.Select(n => n.Id));
        Assert.Empty(list.CompetencyIds);
        list.AddCompetency(bls.Id, Cpr1);
        Assert.True(list.Contains(Cpr1));
    }
}
