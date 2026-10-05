namespace SkillCert.Domain.Reviews;

public enum ReviewOutcome
{
    Competent,
    NotCompetent,
}

/// <summary>How a review was given (spec §7): by the candidate, another user, or a classified reviewer.</summary>
public enum ReviewMethod
{
    Self,
    Peer,
    Classified,
}

/// <summary>
/// The one-time confirmation lifecycle (spec §11). Only <see cref="NotRequired"/> and <see cref="Confirmed"/>
/// reviews count as evidence; a rejected review is kept but never counts and never returns to pending.
/// </summary>
public enum ConfirmationStatus
{
    NotRequired,
    Pending,
    Confirmed,
    Rejected,
}
