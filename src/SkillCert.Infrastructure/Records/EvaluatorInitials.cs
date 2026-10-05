namespace SkillCert.Infrastructure.Records;

/// <summary>
/// Initials for the Initials column and the legend (development plan §2.3): the first letter of each name part,
/// with clashes numbered in order of first appearance (JS, JS2).
/// </summary>
public static class EvaluatorInitials
{
    public static IReadOnlyDictionary<string, string> Assign(IEnumerable<string> namesInOrder)
    {
        var byName = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var name in namesInOrder)
        {
            if (byName.ContainsKey(name))
            {
                continue;
            }

            var initials = Of(name);
            var count = used.GetValueOrDefault(initials) + 1;
            used[initials] = count;
            byName[name] = count == 1 ? initials : $"{initials}{count}";
        }

        return byName;
    }

    public static string Of(string name)
    {
        var letters = name
            .Split([' ', '-', '\''], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => char.IsLetter(part[0]))
            .Select(part => char.ToUpperInvariant(part[0]));
        var initials = string.Concat(letters);
        return initials.Length > 0 ? initials : "?";
    }
}
