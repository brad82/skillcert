using SkillCert.Infrastructure.Records;

namespace SkillCert.Api.Tests.Records;

public sealed class EvaluatorInitialsTests
{
    [Fact]
    public void Initials_are_first_letters_and_clashes_are_numbered_in_order_of_appearance()
    {
        var initials = EvaluatorInitials.Assign(["Ines Instructor", "Sam Supervisor", "Ivan Instructor", "Ines Instructor", "Mary-Jo O'Neil"]);

        Assert.Equal("II", initials["Ines Instructor"]);
        Assert.Equal("II2", initials["Ivan Instructor"]);
        Assert.Equal("SS", initials["Sam Supervisor"]);
        Assert.Equal("MJON", initials["Mary-Jo O'Neil"]);
        Assert.Equal(4, initials.Count);
    }
}
