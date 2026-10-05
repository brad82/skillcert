using System.Globalization;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;

namespace SkillCert.Api.Features.AdminImport;

/// <summary>One CSV row after validation.</summary>
/// <param name="Content">Null when the row has errors.</param>
public sealed record CompetencyCsvRow(
    int Line,
    string Code,
    string Title,
    RevisionContent? Content,
    LowestReviewLevel? LowestReviewer,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record CompetencyCsvResult(IReadOnlyList<string> FileErrors, IReadOnlyList<CompetencyCsvRow> Rows)
{
    /// <summary>Spec §26: any error anywhere blocks the whole import; there is no partial import.</summary>
    public bool CanImport => FileErrors.Count == 0 && Rows.Count > 0 && Rows.All(r => r.Errors.Count == 0);
}

/// <summary>
/// The competency import file contract (spec §26, development plan §2.10). Columns, in any order, headers matched
/// without regard to case:
/// <list type="bullet">
/// <item><c>Code</c>, <c>Title</c>: required.</item>
/// <item><c>ShortTitle</c>, <c>Description</c>: optional text.</item>
/// <item><c>RecertificationDays</c>: blank for no expiry, else a positive whole number (365 = yearly).</item>
/// <item><c>SelfReview</c>, <c>PeerReview</c>, <c>InstructorReview</c>, <c>SupervisorReview</c>: Y/yes/true/1/x or blank/N.
/// The lowest marked level counts; higher levels are implied (decision §2.5).</item>
/// <item><c>Resources</c>: <c>Title|URL|Type</c> entries separated by <c>;</c>; Type is WebPage (default), Video or Document.</item>
/// </list>
/// Whole-file validation: every row is checked, codes must be unique within the file and new to the library
/// (inactive ones included).
/// </summary>
public static class CompetencyCsv
{
    public const int MaxRows = 1000;

    private static readonly string[] Required = ["Code", "Title"];

    private static readonly string[] Known =
        ["Code", "Title", "ShortTitle", "Description", "RecertificationDays", "SelfReview", "PeerReview", "InstructorReview", "SupervisorReview", "Resources"];

    public static CompetencyCsvResult Parse(
        string text, ISet<string> existingNormalizedCodes, IReadOnlyCollection<ReviewerClassification> classifications)
    {
        var records = CsvReader.Read(text);
        if (records.Count == 0)
        {
            return new(["The file is empty."], []);
        }

        var header = records[0].Fields.Select(h => h.Trim()).ToList();
        var column = header.Select((name, index) => (name, index))
            .GroupBy(x => x.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().index, StringComparer.OrdinalIgnoreCase);
        var fileErrors = Required.Where(r => !column.ContainsKey(r)).Select(r => $"Missing the {r} column.").ToList();
        var unknown = header.Where(h => h.Length > 0 && !Known.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();
        if (records.Count - 1 > MaxRows)
        {
            fileErrors.Add($"At most {MaxRows} competencies per file.");
        }

        if (fileErrors.Count > 0)
        {
            return new(fileErrors, []);
        }

        var rows = records.Skip(1).Select(record => ParseRow(record.Line, record.Fields, column, classifications, existingNormalizedCodes)).ToList();
        var duplicates = rows.GroupBy(r => Normalize(r.Code)).Where(g => g.Key.Length > 0 && g.Count() > 1).SelectMany(g => g).ToHashSet();
        rows = rows.Select(r => duplicates.Contains(r)
            ? r with { Errors = [.. r.Errors, $"Code {r.Code} appears more than once in this file."], Content = null }
            : r).ToList();
        if (unknown.Count > 0 && rows.Count > 0)
        {
            rows[0] = rows[0] with { Warnings = [.. rows[0].Warnings, $"Ignored unknown columns: {string.Join(", ", unknown)}."] };
        }

        return new([], rows);
    }

    private static string Normalize(string code) => string.IsNullOrWhiteSpace(code) ? string.Empty : Competency.Normalize(code);

    private static CompetencyCsvRow ParseRow(
        int line,
        IReadOnlyList<string> fields,
        Dictionary<string, int> column,
        IReadOnlyCollection<ReviewerClassification> classifications,
        ISet<string> existingNormalizedCodes)
    {
        string Cell(string name) => column.TryGetValue(name, out var index) && index < fields.Count ? fields[index].Trim() : string.Empty;
        var errors = new List<string>();
        var warnings = new List<string>();

        var code = Cell("Code");
        var title = Cell("Title");
        if (code.Length == 0)
        {
            errors.Add("Code is required.");
        }
        else if (code.Length > 50)
        {
            errors.Add("Code is longer than 50 characters.");
        }
        else if (existingNormalizedCodes.Contains(Competency.Normalize(code)))
        {
            errors.Add($"Code {code} already exists in the library.");
        }

        if (title.Length == 0)
        {
            errors.Add("Title is required.");
        }
        else if (title.Length > 300)
        {
            errors.Add("Title is longer than 300 characters.");
        }

        var shortTitle = Cell("ShortTitle");
        var description = Cell("Description");
        if (shortTitle.Length > 300)
        {
            errors.Add("ShortTitle is longer than 300 characters.");
        }

        if (description.Length > 4000)
        {
            errors.Add("Description is longer than 4000 characters.");
        }

        int? days = null;
        var daysText = Cell("RecertificationDays");
        if (daysText.Length == 0)
        {
            warnings.Add("No RecertificationDays: this skill never expires.");
        }
        else if (int.TryParse(daysText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed is >= 1 and <= 36_500)
        {
            days = parsed;
        }
        else
        {
            errors.Add($"RecertificationDays must be a whole number of days from 1 to 36500 (got \"{daysText}\").");
        }

        var levels = new (string Column, ReviewMethod Method, string? Classification)[]
        {
            ("SelfReview", ReviewMethod.Self, null),
            ("PeerReview", ReviewMethod.Peer, null),
            ("InstructorReview", ReviewMethod.Classified, "Instructor"),
            ("SupervisorReview", ReviewMethod.Classified, "Supervisor"),
        };
        var marked = new List<int>();
        var badFlag = false;
        for (var i = 0; i < levels.Length; i++)
        {
            switch (Flag(Cell(levels[i].Column)))
            {
                case true:
                    marked.Add(i);
                    break;
                case null:
                    errors.Add($"{levels[i].Column} must be Y or blank (got \"{Cell(levels[i].Column)}\").");
                    badFlag = true;
                    break;
            }
        }

        if (marked.Count == 0 && !badFlag)
        {
            errors.Add("Mark at least one review method (SelfReview, PeerReview, InstructorReview or SupervisorReview).");
        }
        else if (marked.Count > 0 && marked.Count < levels.Length - marked[0])
        {
            warnings.Add($"Every level above {levels[marked[0]].Column.Replace("Review", string.Empty, StringComparison.Ordinal)} may sign too (review hierarchy).");
        }

        var resources = new List<RevisionResource>();
        foreach (var entry in Cell("Resources").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split('|', StringSplitOptions.TrimEntries);
            var type = ResourceType.WebPage;
            if (parts.Length is < 2 or > 3 || parts[0].Length is 0 or > 200
                || !Uri.TryCreate(parts[1], UriKind.Absolute, out var url) || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp)
                || (parts.Length == 3 && !Enum.TryParse(parts[2].Replace(" ", string.Empty, StringComparison.Ordinal), ignoreCase: true, out type)))
            {
                errors.Add($"Resource \"{entry}\" must be Title|https://link|Type (WebPage, Video or Document).");
                continue;
            }

            resources.Add(new RevisionResource(parts[0], url, type));
        }

        if (resources.Count > 20)
        {
            errors.Add("At most 20 resources per competency.");
        }

        RevisionContent? content = null;
        LowestReviewLevel? lowest = null;
        if (errors.Count == 0)
        {
            var (_, method, classificationCode) = levels[marked[0]];
            var classification = classificationCode is null ? null : classifications.SingleOrDefault(c => c.Code == classificationCode);
            if (classificationCode is not null && classification is null)
            {
                errors.Add($"The {classificationCode} classification isn't configured.");
            }
            else
            {
                content = ReviewHierarchy.CloseUpward(
                    new RevisionContent(
                        title,
                        shortTitle.Length == 0 ? null : shortTitle,
                        description.Length == 0 ? null : description,
                        days,
                        AllowsSelfReview: method == ReviewMethod.Self,
                        AllowsPeerReview: method == ReviewMethod.Peer,
                        classification is null ? [] : [classification.Id],
                        resources),
                    classifications);
                lowest = new LowestReviewLevel(method, classification);
            }
        }

        return new CompetencyCsvRow(line, code, title, content, lowest, errors, warnings);
    }

    /// <returns>true for Y/yes/true/1/x, false for blank/N/no/false/0, null otherwise.</returns>
    private static bool? Flag(string value) => value.ToUpperInvariant() switch
    {
        "Y" or "YES" or "TRUE" or "1" or "X" => true,
        "" or "N" or "NO" or "FALSE" or "0" => false,
        _ => null,
    };
}
