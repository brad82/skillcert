using SkillCert.Domain.Currency;

namespace SkillCert.Domain.Compliance;

/// <summary>One competency's evidence: everything currency needs, for one user.</summary>
public sealed record CompetencyEvidence(IReadOnlyCollection<ReviewEvidence> Reviews, IReadOnlyCollection<RevisionPolicy> Revisions);

/// <summary>
/// Compliance and completion for one user against one list (spec §21). Pure: give it the evidence for every
/// distinct competency the list contains now.
/// </summary>
public static class ListCompliance
{
    /// <summary>A non-empty list is compliant only when every distinct competency in it is Current. Empty lists never are.</summary>
    public static bool IsCompliant(IReadOnlyCollection<CompetencyCurrency> currencies) =>
        currencies.Count > 0 && currencies.All(c => c.IsCurrent);

    /// <summary>
    /// The start of the period, ending at <paramref name="asOf"/>, in which every competency in the list has been
    /// Current at the same time: the latest of each competency's <see cref="CurrentSince"/>. Evidence-based, so a
    /// confirmation, an assignment or the rendering time never moves it. Null when the list isn't compliant.
    /// </summary>
    /// <remarks>
    /// Uses the list as it is now: a requirement removed today simply isn't passed in, which gives the spec's
    /// "remaining competencies achieved 1 Sept, missing requirement removed 4 Oct → completed 1 Sept".
    /// </remarks>
    public static DateTimeOffset? CompletionDate(IReadOnlyCollection<CompetencyEvidence> competencies, DateTimeOffset asOf)
    {
        if (competencies.Count == 0)
        {
            return null;
        }

        DateTimeOffset? latest = null;
        foreach (var competency in competencies)
        {
            if (CurrentSince(competency, asOf) is not { } since)
            {
                return null;
            }

            latest = latest is null || since > latest ? since : latest;
        }

        return latest;
    }

    /// <summary>
    /// When the competency's unbroken run of currency up to <paramref name="asOf"/> began, or null when it isn't
    /// Current. A renewal before expiry continues the run; a lapse, a Not Competent or a breaking revision ends it.
    /// </summary>
    public static DateTimeOffset? CurrentSince(CompetencyEvidence competency, DateTimeOffset asOf)
    {
        var currency = CurrencyEvaluator.Evaluate(competency.Reviews, competency.Revisions, asOf);
        if (!currency.IsCurrent)
        {
            return null;
        }

        // Step back to just before the run's start. If still current there, the same evidence held on the whole
        // stretch between (no other accepted review lies inside it), so the run extends to that earlier start.
        var since = currency.AchievedAt!.Value;
        while (true)
        {
            var before = CurrencyEvaluator.Evaluate(competency.Reviews, competency.Revisions, since.AddTicks(-1));
            if (!before.IsCurrent)
            {
                return since;
            }

            since = before.AchievedAt!.Value;
        }
    }
}
