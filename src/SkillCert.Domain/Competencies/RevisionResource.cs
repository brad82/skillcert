namespace SkillCert.Domain.Competencies;

/// <summary>A titled URL attached to a competency revision, shown to candidates as learning material.</summary>
public sealed record RevisionResource
{
    public RevisionResource(string title, Uri url, ResourceType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(url);
        if (!url.IsAbsoluteUri || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Resources must be absolute http(s) URLs.", nameof(url));
        }

        Title = title.Trim();
        Url = url;
        Type = type;
    }

    public string Title { get; }

    public Uri Url { get; }

    public ResourceType Type { get; }
}
