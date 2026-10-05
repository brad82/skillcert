using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;

namespace SkillCert.Domain.Tests.TestData;

/// <summary>Valid revision content with sensible defaults; override what a test cares about.</summary>
public static class Content
{
    public static RevisionContent Revision(
        string title = "CPR – one-rescuer adult",
        int? recertificationDays = 365,
        bool self = false,
        bool peer = false,
        Guid[]? classifications = null,
        string? shortTitle = null) =>
        new(
            title,
            shortTitle,
            Description: null,
            recertificationDays,
            self,
            peer,
            classifications ?? [ReviewerClassification.InstructorId],
            Resources: []);
}
