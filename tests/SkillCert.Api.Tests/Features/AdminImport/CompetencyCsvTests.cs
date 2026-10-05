using SkillCert.Api.Features.AdminImport;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;

namespace SkillCert.Api.Tests.Features.AdminImport;

/// <summary>The file contract on its own, without a database (spec §26).</summary>
public sealed class CompetencyCsvTests
{
    private static readonly ReviewerClassification[] Classifications =
    [
        new(ReviewerClassification.InstructorId, "Instructor", "Instructor", AffirmationPolicy.Automatic, 10),
        new(ReviewerClassification.SupervisorId, "Supervisor", "Supervisor", AffirmationPolicy.ReviewerConfirmation, 20),
    ];

    private const string Header = "Code,Title,ShortTitle,Description,RecertificationDays,SelfReview,PeerReview,InstructorReview,SupervisorReview,Resources";

    private static CompetencyCsvResult Parse(string body, params string[] existing) =>
        CompetencyCsv.Parse($"{Header}\n{body}", existing.Select(Competency.Normalize).ToHashSet(), Classifications);

    [Fact]
    public void Quoted_fields_keep_commas_quotes_and_line_breaks()
    {
        var records = CsvReader.Read("﻿a,\"b, \"\"c\"\"\",\"d\r\ne\"\r\n\r\nf,g\n");

        Assert.Equal(["a", "b, \"c\"", "d\r\ne"], records[0].Fields);
        Assert.Equal(4, records[1].Line);
        Assert.Equal(["f", "g"], records[1].Fields);
    }

    [Fact]
    public void A_valid_row_becomes_closed_upward_content_with_resources()
    {
        var result = Parse("13.1,Transceiver search,Search,Find a buried transceiver,365,,,Y,,\"Search basics|https://example.org/t|Video;Card|https://example.org/c.pdf|document\"");

        var row = Assert.Single(result.Rows);
        Assert.True(result.CanImport);
        Assert.Equal((ReviewMethod.Classified, "Instructor"), (row.LowestReviewer!.Method, row.LowestReviewer.Classification!.Code));
        Assert.Equal(2, row.Content!.PermittedClassificationIds.Count); // Supervisor implied
        Assert.Equal([ResourceType.Video, ResourceType.Document], row.Content.Resources.Select(r => r.Type));
        Assert.Contains(row.Warnings, w => w.Contains("may sign too", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_rule_is_reported_on_its_row_and_any_error_blocks_the_file()
    {
        var result = Parse(string.Join('\n',
            "13.1,Fine,,,,Y,,,,",
            ",No code,,,365,,Y,,,",
            "13.2,,,,0,,,,maybe,",
            "13.3,Bad link,,,365,,,,Y,Guide|ftp://x"));

        Assert.False(result.CanImport);
        Assert.Empty(result.Rows[0].Errors);
        Assert.Contains("No RecertificationDays: this skill never expires.", result.Rows[0].Warnings);
        Assert.Contains("Code is required.", result.Rows[1].Errors);
        Assert.Equal(3, result.Rows[2].Errors.Count); // title, days, flag
        Assert.Single(result.Rows[3].Errors);
        Assert.Equal([2, 3, 4, 5], result.Rows.Select(r => r.Line));
    }

    [Fact]
    public void Codes_must_be_new_and_unique_within_the_file_after_normalising()
    {
        var result = Parse("13.1,One,,,365,Y,,,,\n 13.1 ,Two,,,365,Y,,,,\n4.3.1,Taken,,,365,Y,,,,", "4.3.1");

        Assert.All(result.Rows, r => Assert.NotEmpty(r.Errors));
        Assert.Contains("already exists", result.Rows[2].Errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_required_column_is_a_file_error()
    {
        var result = CompetencyCsv.Parse("Code,Name\n1,x", new HashSet<string>(), Classifications);

        Assert.Equal(["Missing the Title column."], result.FileErrors);
        Assert.False(result.CanImport);
    }
}
