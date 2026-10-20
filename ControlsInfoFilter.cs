namespace Designer.Modules.ComponentControlsInfoFilter;

/// <summary>Builds a filtered display without changing the complete source text.</summary>
internal static class ControlsInfoFilter
{
    internal readonly record struct Result(string Text, string CountText, bool IsControlList);

    internal static Result Apply(string sourceText, string query)
    {
        var lines = sourceText.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Preserve the existing row-shape check, including localized host messages.
        if (lines.Length > 0 && !lines.All(line => line.Contains(" ) ( ", StringComparison.Ordinal)))
            return new Result(sourceText, "", false);

        var words = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var matches = lines.Where(line => words.All(word => line.Contains(word, StringComparison.OrdinalIgnoreCase))).ToArray();
        var text = string.Join("\r\n", matches);
        var countText = matches.Length == 0 && lines.Length > 0 ? $"No matching controls (0 of {lines.Length})"
            : $"{matches.Length} of {lines.Length} controls";
        return new Result(text, countText, true);
    }
}
