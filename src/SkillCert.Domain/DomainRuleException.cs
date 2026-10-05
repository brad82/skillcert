namespace SkillCert.Domain;

/// <summary>
/// A business rule refused an operation. <see cref="Code"/> is stable so the API can map it to a problem
/// response the web app branches on (api-architecture.md, "Responses and errors").
/// </summary>
public sealed class DomainRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
