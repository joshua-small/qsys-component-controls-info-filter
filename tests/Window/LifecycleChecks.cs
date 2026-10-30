using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class LifecycleChecks
{
    internal static void Add(List<(string Name, Action Check)> tests)
    {
        tests.Add(("docked-first inspector configures its first floating host", () =>
        {
            using var session = new Session();
            session.MoveTo(session.Main);
            session.AssertMainUnchanged();
            var search = session.Search;
            search.Text = "Main Float";
            var floating = session.CreateHost();
            session.MoveTo(floating);
            AssertConfigured(floating);
            session.AssertFilterPreserved(search);
            session.AssertMainUnchanged();
        }));

        tests.Add(("float-dock-refloat configures a recreated host and preserves the filter", () =>
        {
            using var session = new Session();
            var first = session.CreateHost();
            session.MoveTo(first);
            AssertConfigured(first);
            var search = session.Search;
            search.Text = "Main Float";
            session.MoveTo(session.Main);
            ResetHost(first);
            var second = session.CreateHost();
            session.MoveTo(second);
            AssertConfigured(second);
            AssertUnconfigured(first);
            session.AssertMainUnchanged();
            session.AssertFilterPreserved(search);
        }));

        tests.Add(("float-dock-refloat reapplies properties to a reused host", () =>
        {
            using var session = new Session();
            var floating = session.CreateHost();
            session.MoveTo(floating);
            session.MoveTo(session.Main);
            ResetHost(floating);
            session.MoveTo(floating);
            AssertConfigured(floating);
            session.AssertMainUnchanged();
        }));

        tests.Add(("close and reopen with the retained inspector configures the new host", () =>
        {
            using var session = new Session();
            var first = session.CreateHost();
            session.MoveTo(first);
            var search = session.Search;
            search.Text = "Main Float";
            session.Detach();
            first.Close();
            var reopened = session.CreateHost();
            session.MoveTo(reopened);
            AssertConfigured(reopened);
            session.AssertFilterPreserved(search);
        }));

        tests.Add(("queued refresh resolves the current host after rapid reparenting", () =>
        {
            using var session = new Session();
            session.MoveTo(session.Main);
            var transient = session.CreateHost();
            session.MoveTo(transient, drain: false);
            Drain(DispatcherPriority.Loaded);
            if (!session.Control.IsLoaded) throw new Exception("Loaded precondition was not reached.");
            session.MoveTo(session.Main, drain: false);
            ResetHost(transient);
            Drain();
            session.AssertMainUnchanged();
            AssertUnconfigured(transient);
        }));

        tests.Add(("unloaded inspector leaves retired and unrelated windows alone", () =>
        {
            using var session = new Session();
            var retired = session.CreateHost();
            session.MoveTo(retired);
            session.Detach();
            ResetHost(retired);
            var unrelated = session.CreateHost();
            unrelated.Show();
            Drain();
            AssertUnconfigured(retired);
            AssertUnconfigured(unrelated);
            session.MoveTo(unrelated);
            AssertConfigured(unrelated);
        }));
    }

    private static void Drain(DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(priority, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static void ResetHost(Window host)
    {
        host.Title = "Native floating host";
        host.ShowInTaskbar = false;
        host.WindowStyle = WindowStyle.None;
    }

    private static void AssertConfigured(Window host)
    {
        if (host.Title != "Component Controls Info" || !host.ShowInTaskbar
            || host.WindowStyle != WindowStyle.SingleBorderWindow)
            throw new Exception($"Host was not configured: {host.Title}, {host.WindowStyle}, {host.ShowInTaskbar}.");
    }

    private static void AssertUnconfigured(Window host)
    {
        if (host.Title != "Native floating host" || host.ShowInTaskbar || host.WindowStyle != WindowStyle.None)
            throw new Exception("A retired or unrelated host was changed.");
    }

    private sealed class Session : IDisposable
    {
        private readonly List<Window> windows = new();
        private Window? current;
        internal ComponentInfoFixture Control { get; } = new();
        internal Window Main { get; }
        internal TextBox Search => Descendants(Control).OfType<TextBox>()
            .Single(box => box.Name == "ComponentInfoSearchText");

        internal Session()
        {
            Main = CreateHost();
            Main.Title = "Original design title";
            Main.WindowStyle = WindowStyle.ToolWindow;
            Application.Current.MainWindow = Main;
            Main.Show();
            Drain();
        }

        internal Window CreateHost()
        {
            // Real presentation sources, kept off-screen without requesting focus.
            var host = new Window { Width = 250, Height = 200, Left = -30000, Top = -30000,
                ShowActivated = false, WindowStartupLocation = WindowStartupLocation.Manual };
            ResetHost(host);
            windows.Add(host);
            host.Closed += (_, _) => windows.Remove(host);
            return host;
        }

        internal void MoveTo(Window target, bool drain = true)
        {
            if (current is not null) current.Content = null;
            target.Content = Control;
            current = target;
            if (!target.IsVisible) target.Show();
            if (drain) Drain();
        }

        internal void Detach()
        {
            if (current is not null) current.Content = null;
            current = null;
            Drain();
            if (Control.IsLoaded) throw new Exception("Unloaded precondition was not reached.");
        }

        internal void AssertMainUnchanged()
        {
            if (Main.Title != "Original design title" || Main.ShowInTaskbar || Main.WindowStyle != WindowStyle.ToolWindow)
                throw new Exception("Main-window exclusion failed during reparenting.");
        }

        internal void AssertFilterPreserved(TextBox search)
        {
            if (!ReferenceEquals(search, Search) || Search.Text != "Main Float")
                throw new Exception("Filter state was replaced or lost.");
            var display = Descendants(Control).OfType<TextBox>().Single(box => box.Name == "ComponentInfoFilteredText");
            if (display.Text != "gain.1 ( Main Level ) ( Float )" || Control.SourceText != ComponentInfoFixture.CompleteSource)
                throw new Exception("Filtered display or complete source changed.");
            if (((Grid)Control.Content).Children.Count != 3)
                throw new Exception("Filter UI was duplicated.");
        }

        public void Dispose()
        {
            Detach();
            foreach (var window in windows.ToArray()) window.Close();
            Drain();
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
    }
}
