using System.Globalization;
using System.Text;

namespace SkillCert.Domain.Reviews;

/// <summary>A point on the signature pad, in pad pixels from the top left.</summary>
public readonly record struct SignaturePoint(double X, double Y);

/// <summary>
/// A signature as captured on the candidate's device: the pad size and the pen strokes (spec §10). The server
/// turns it into SVG itself, so stored signatures never contain client-supplied markup.
/// </summary>
public sealed record SignatureDrawing
{
    public const string Invalid = "signature.invalid";
    public const int MaxSide = 4000;
    public const int MaxStrokes = 200;
    public const int MaxPoints = 10_000;

    public SignatureDrawing(int width, int height, IReadOnlyList<IReadOnlyList<SignaturePoint>> strokes)
    {
        if (width is <= 0 or > MaxSide || height is <= 0 or > MaxSide)
        {
            throw new DomainRuleException(Invalid, "The signature pad size is out of range.");
        }

        if (strokes.Count is 0 or > MaxStrokes || strokes.Any(s => s.Count == 0) || strokes.Sum(s => s.Count) > MaxPoints)
        {
            throw new DomainRuleException(Invalid, "The signature is empty or too large.");
        }

        if (strokes.SelectMany(s => s).Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)
            || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
        {
            throw new DomainRuleException(Invalid, "The signature has points outside the pad.");
        }

        Width = width;
        Height = height;
        Strokes = strokes;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<IReadOnlyList<SignaturePoint>> Strokes { get; }

    /// <summary>Dark round-capped strokes on a transparent background; a single-point stroke draws a dot.</summary>
    public string ToSvg()
    {
        var path = new StringBuilder();
        foreach (var stroke in Strokes)
        {
            path.Append('M').Append(Format(stroke[0]));
            foreach (var point in stroke.Count == 1 ? stroke : stroke.Skip(1))
            {
                path.Append('L').Append(Format(point));
            }
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\"><path d=\"{path}\" fill=\"none\" stroke=\"#1f1a1b\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>");
    }

    private static string Format(SignaturePoint point) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(point.X, 1)} {Math.Round(point.Y, 1)}");
}
