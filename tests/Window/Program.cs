using System.Windows;
using Designer.Modules.ComponentControlsInfoFilter;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var attachmentErrors = new List<string>();
        ControlsInfoSearch.Install((kind, detail) => attachmentErrors.Add($"{kind}: {detail}"));
        var tests = new List<(string Name, Action Check)>
        {
            ("missing host is a no-op", () =>
            {
                var main = CreateWindow("Design - saved", WindowStyle.ToolWindow, false);
                ControlsInfoWindow.Configure(null, main);
                AssertWindow(main, "Design - saved", WindowStyle.ToolWindow, false);
                ControlsInfoWindow.Configure(null, null);
            }),
            ("floating host gets title, style and taskbar visibility", () =>
            {
                var main = CreateWindow("Design - saved", WindowStyle.ThreeDBorderWindow, false);
                var floating = CreateWindow("", WindowStyle.ToolWindow, false);
                var content = new object();
                floating.Content = content;
                ControlsInfoWindow.Configure(floating, main);
                AssertWindow(floating, "Component Controls Info", WindowStyle.SingleBorderWindow, true);
                AssertWindow(main, "Design - saved", WindowStyle.ThreeDBorderWindow, false);
                if (!ReferenceEquals(content, floating.Content)) throw new Exception("Content changed.");
            }),
            ("identity determines exclusion, not matching titles", () =>
            {
                var main = CreateWindow("Component Controls Info", WindowStyle.ToolWindow, false);
                var floating = CreateWindow(main.Title, WindowStyle.None, false);
                ControlsInfoWindow.Configure(floating, main);
                AssertWindow(floating, "Component Controls Info", WindowStyle.SingleBorderWindow, true);
                AssertWindow(main, "Component Controls Info", WindowStyle.ToolWindow, false);
            }),
            ("repeated configuration keeps the same result", () =>
            {
                var floating = CreateWindow("", WindowStyle.ToolWindow, false);
                ControlsInfoWindow.Configure(floating, null);
                ControlsInfoWindow.Configure(floating, null);
                AssertWindow(floating, "Component Controls Info", WindowStyle.SingleBorderWindow, true);
            })
        };

        foreach (var style in Enum.GetValues<WindowStyle>())
        foreach (var showInTaskbar in new[] { false, true })
        {
            tests.Add(($"main window excluded: {style}, taskbar={showInTaskbar}", () =>
            {
                var main = CreateWindow("Original design title", style, showInTaskbar);
                ControlsInfoWindow.Configure(main, main);
                AssertWindow(main, "Original design title", style, showInTaskbar);
            }));
        }

        LifecycleChecks.Add(tests);
        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                var errorsBefore = attachmentErrors.Count;
                test.Check();
                if (attachmentErrors.Count != errorsBefore)
                    throw new Exception(string.Join("; ", attachmentErrors.Skip(errorsBefore)));
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception error)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {test.Name}: {error.Message}");
            }
        }
        Console.WriteLine($"{tests.Count - failed}/{tests.Count} WPF window regression cases passed.");
        application.Shutdown();
        return failed == 0 ? 0 : 1;
    }

    private static Window CreateWindow(string title, WindowStyle style, bool showInTaskbar)
        => new() { Title = title, WindowStyle = style, ShowInTaskbar = showInTaskbar };

    private static void AssertWindow(Window window, string title, WindowStyle style, bool showInTaskbar)
    {
        if (window.Title != title || window.WindowStyle != style || window.ShowInTaskbar != showInTaskbar)
            throw new Exception($"Unexpected window properties: {window.Title}, {window.WindowStyle}, {window.ShowInTaskbar}.");
    }
}
