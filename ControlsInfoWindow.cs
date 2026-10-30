using System.Windows;
using System.Windows.Threading;

namespace Designer.Modules.ComponentControlsInfoFilter;

internal static class ControlsInfoWindow
{
    private static readonly DependencyProperty TrackerProperty = DependencyProperty.RegisterAttached(
        "HostTracker", typeof(HostTracker), typeof(ControlsInfoWindow), new PropertyMetadata(null));

    internal static void Track(FrameworkElement owner)
    {
        if (owner.GetValue(TrackerProperty) is null)
            owner.SetValue(TrackerProperty, new HostTracker(owner));
    }

    internal static void Configure(Window? floating, Window? mainWindow)
    {
        if (floating is null || ReferenceEquals(floating, mainWindow)) return;

        floating.Title = "Component Controls Info";
        floating.ShowInTaskbar = true;
        floating.WindowStyle = WindowStyle.SingleBorderWindow;
    }

    private sealed class HostTracker
    {
        private readonly FrameworkElement owner;
        private DispatcherOperation? pending;
        private bool observingSource;

        internal HostTracker(FrameworkElement owner)
        {
            this.owner = owner;
            owner.Loaded += OnLoaded;
            owner.Unloaded += OnUnloaded;
            if (owner.IsLoaded) ObserveSource();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => ObserveSource();

        private void ObserveSource()
        {
            if (!observingSource)
            {
                PresentationSource.AddSourceChangedHandler(owner, OnSourceChanged);
                observingSource = true;
            }
            QueueRefresh();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (observingSource)
            {
                PresentationSource.RemoveSourceChangedHandler(owner, OnSourceChanged);
                observingSource = false;
            }
            pending?.Abort();
            pending = null;
        }

        private void OnSourceChanged(object sender, SourceChangedEventArgs e) => QueueRefresh();

        private void QueueRefresh()
        {
            if (pending?.Status == DispatcherOperationStatus.Pending) return;
            pending = owner.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                pending = null;
                // Resolve the current host after reparenting, never a captured old window.
                if (owner.IsLoaded) Configure(Window.GetWindow(owner), Application.Current?.MainWindow);
            }));
        }
    }
}
