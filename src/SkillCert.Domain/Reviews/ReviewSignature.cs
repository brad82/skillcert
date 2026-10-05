namespace SkillCert.Domain.Reviews;

/// <summary>
/// A captured touchscreen signature, stored once and shared by every review created in the same interaction
/// (spec §10). Associated evidence, not cryptographic identity. Data is server-rendered SVG, never client SVG.
/// </summary>
public sealed class ReviewSignature
{
    public const string SvgContentType = "image/svg+xml";

    private ReviewSignature()
    {
    }

    private ReviewSignature(string data, string contentType, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        Id = Guid.CreateVersion7(createdAt);
        Data = data;
        ContentType = contentType;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Data { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static ReviewSignature FromServerRenderedSvg(string svg, DateTimeOffset createdAt) =>
        new(svg, SvgContentType, createdAt);
}
