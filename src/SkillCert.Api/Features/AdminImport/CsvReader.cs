using System.Text;

namespace SkillCert.Api.Features.AdminImport;

/// <summary>
/// Minimal RFC 4180 reader: comma-separated, double-quoted fields may contain commas, quotes ("") and line breaks.
/// Accepts CRLF or LF and ignores a UTF-8 byte-order mark. Blank lines are skipped.
/// </summary>
public static class CsvReader
{
    /// <returns>Each record with the 1-based line it started on.</returns>
    public static IReadOnlyList<(int Line, IReadOnlyList<string> Fields)> Read(string text)
    {
        var records = new List<(int, IReadOnlyList<string>)>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var (line, recordLine, quoted, fieldStarted) = (1, 1, false, false);
        var i = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

        void EndField()
        {
            fields.Add(field.ToString());
            field.Clear();
            fieldStarted = false;
        }

        void EndRecord()
        {
            EndField();
            if (fields.Count > 1 || fields[0].Length > 0)
            {
                records.Add((recordLine, fields.ToList()));
            }

            fields.Clear();
        }

        for (; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    if (c == '\n')
                    {
                        line++;
                    }

                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"' when !fieldStarted && field.Length == 0:
                    quoted = true;
                    fieldStarted = true;
                    break;
                case ',':
                    EndField();
                    break;
                case '\r':
                    break;
                case '\n':
                    EndRecord();
                    line++;
                    recordLine = line;
                    break;
                default:
                    field.Append(c);
                    fieldStarted = true;
                    break;
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            EndRecord();
        }

        return records;
    }
}
