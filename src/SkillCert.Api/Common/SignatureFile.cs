using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SkillCert.Api.Common;

/// <summary>
/// Serves a stored, server-rendered signature SVG. The CSP blocks scripts, so the file is safe even when opened
/// directly rather than through an &lt;img&gt;.
/// </summary>
public static class SignatureFile
{
    public static FileContentHttpResult Serve(HttpContext http, string data, string contentType)
    {
        http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'";
        http.Response.Headers.XContentTypeOptions = "nosniff";
        http.Response.Headers.CacheControl = "private, max-age=86400, immutable";
        return TypedResults.File(Encoding.UTF8.GetBytes(data), contentType);
    }
}
