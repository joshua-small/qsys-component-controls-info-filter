using System.Globalization;
using Designer.Modules.ComponentControlsInfoFilter;

// Synthetic rows exercise the existing row-shape heuristic, not a host schema.
const string level = "gain.1 ( Main Level ) ( Float )";
const string mute = "mute.1 ( Main Mute ) ( Boolean )";
const string aux = "gain.2 ( Aux Level ) ( Float )";
const string rows = level + "\r\n" + mute + "\r\n" + aux;
var tests = new List<(string Name, Action Check)>();

void Equal<T>(T expected, T actual, string field)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{field}: expected <{expected}>, got <{actual}>.");
}

void Check(string source, string query, string expectedText, string expectedCount, bool isControlList = true)
{
    var result = ControlsInfoFilter.Apply(source, query);
    Equal(expectedText, result.Text, "text");
    Equal(expectedCount, result.CountText, "count");
    Equal(isControlList, result.IsControlList, "control-list classification");
}

void Case(string name, string source, string query, string expectedText, string expectedCount, bool isControlList = true)
    => tests.Add((name, () => Check(source, query, expectedText, expectedCount, isControlList)));

Case("empty source", "", "", "", "0 of 0 controls");
Case("empty source with query", "", "gain", "", "0 of 0 controls");
Case("only empty lines", "\r\n\n\r\n", "", "", "0 of 0 controls");
Case("single row", level, "", level, "1 of 1 controls");
Case("empty query shows all rows", rows, "", rows, "3 of 3 controls");
Case("whitespace query shows all rows", rows, " \t\r\n\u00a0 ", rows, "3 of 3 controls");
Case("name match", rows, "mute.1", mute, "1 of 3 controls");
Case("label match", rows, "aux", aux, "1 of 3 controls");
Case("type match preserves source order", rows, "Float", level + "\r\n" + aux, "2 of 3 controls");
Case("case insensitive matching", rows, "mAiN bOoLeAn", mute, "1 of 3 controls");
Case("all query terms match one row", rows, "Main Float", level, "1 of 3 controls");
Case("query term order is irrelevant", rows, "Float Main", level, "1 of 3 controls");
Case("terms cannot match different rows", rows, "Aux Boolean", "", "No matching controls (0 of 3)");
Case("no matches", rows, "absent", "", "No matching controls (0 of 3)");
Case("single row rejection", level, "Boolean", "", "No matching controls (0 of 1)");
Case("substrings match", rows, "ain.1 evel", level, "1 of 3 controls");
Case("repeated terms", rows, "main main float", level, "1 of 3 controls");
Case("trimmed and repeated query whitespace", rows, "  Main \t Float  ", level, "1 of 3 controls");
Case("multiline query", rows, "\r\nMain\nFloat\r\n", level, "1 of 3 controls");
Case("unicode query whitespace", rows, "Main\u00a0Float", level, "1 of 3 controls");
Case("punctuation is literal", rows, "gain.1", level, "1 of 3 controls");
Case("no wildcard syntax", rows, "gain*", "", "No matching controls (0 of 3)");
Case("no regular expression syntax", rows, "gain.*", "", "No matching controls (0 of 3)");
Case("quotes are literal", rows, "\"Main\"", "", "No matching controls (0 of 3)");
Case("LF source uses CRLF display", level + "\n" + mute, "", level + "\r\n" + mute, "2 of 2 controls");
Case("mixed newlines and empty lines", "\n" + level + "\r\n\r\n" + mute + "\n\n" + aux + "\r\n",
    "", rows, "3 of 3 controls");
Case("duplicate rows remain counted", level + "\n" + level + "\n" + mute, "Float",
    level + "\r\n" + level, "2 of 3 controls");
Case("matching row spacing is preserved", " \t" + level + "  \t", "Main",
    " \t" + level + "  \t", "1 of 1 controls");
Case("lone carriage return is preserved", level + "\r" + mute, "Boolean",
    level + "\r" + mute, "1 of 1 controls");
Case("localized message bypasses query", "Sélectionnez un seul composant.", "gain",
    "Sélectionnez un seul composant.", "", false);
Case("message line endings and spacing stay exact", "\r\n  Select one component.\n\n", "absent",
    "\r\n  Select one component.\n\n", "", false);
Case("mixed message and rows pass through in full", level + "\nSelect one component.\r\n" + mute, "Float",
    level + "\nSelect one component.\r\n" + mute, "", false);
Case("whitespace-only source is a message", " \t\r\n", "", " \t\r\n", "", false);
Case("whitespace-only line prevents row classification", level + "\n \t\n" + mute, "gain",
    level + "\n \t\n" + mute, "", false);
Case("missing delimiter passes through", "gain.1 Main Level Float", "absent",
    "gain.1 Main Level Float", "", false);
Case("near delimiter does not count as a row", "gain.1 ( Main Level )( Float )", "absent",
    "gain.1 ( Main Level )( Float )", "", false);
Case("one incomplete row preserves the entire source", rows + "\nincomplete", "Main",
    rows + "\nincomplete", "", false);

tests.Add(("matching is independent of current culture", () =>
{
    var previous = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        const string row = "INPUT ( Main Level ) ( Float )";
        Check(row, "input", row, "1 of 1 controls");
    }
    finally
    {
        CultureInfo.CurrentCulture = previous;
    }
}));

tests.Add(("successive sources and queries have no stale results", () =>
{
    Check(rows, "Main", level + "\r\n" + mute, "2 of 3 controls");
    Check(aux, "Main", "", "No matching controls (0 of 1)");
    Check("Select one component.", "Main", "Select one component.", "", false);
    Check("", "Main", "", "0 of 0 controls");
    Check(rows, "", rows, "3 of 3 controls");
}));

tests.Add(("large list filters and clears from the complete source", () =>
{
    var source = string.Join("\n", Enumerable.Range(0, 2000)
        .Select(index => $"control.{index:D4} ( Synthetic Level ) ( Float )"));
    Check(source, "control.1999 Float", "control.1999 ( Synthetic Level ) ( Float )", "1 of 2000 controls");
    Check(source, "control.2000", "", "No matching controls (0 of 2000)");
    Check(source, "", source.Replace("\n", "\r\n"), "2000 of 2000 controls");
}));

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Check();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception error)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {error.Message}");
    }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} portable regression cases passed.");
return failed == 0 ? 0 : 1;
