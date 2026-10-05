namespace SkillCert.Api.Common;

/// <summary>Orders codes the way people read them: 9.1a before 11.1, 10.2 before 10.10. Display only.</summary>
public sealed class NaturalCodeComparer : IComparer<string>
{
    public static readonly NaturalCodeComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null || y is null)
        {
            return x is null ? -1 : 1;
        }

        var (i, j) = (0, 0);
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var (startX, startY) = (i, j);
                while (i < x.Length && char.IsDigit(x[i]))
                {
                    i++;
                }

                while (j < y.Length && char.IsDigit(y[j]))
                {
                    j++;
                }

                var byNumber = decimal.Parse(x.AsSpan(startX, i - startX), System.Globalization.CultureInfo.InvariantCulture).CompareTo(decimal.Parse(y.AsSpan(startY, j - startY), System.Globalization.CultureInfo.InvariantCulture));
                if (byNumber != 0)
                {
                    return byNumber;
                }

                continue;
            }

            var byChar = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
            if (byChar != 0)
            {
                return byChar;
            }

            i++;
            j++;
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }
}
