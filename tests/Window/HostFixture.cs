using System.Windows.Controls;

namespace QsysMods.Core
{
    // Only host-type lookup is substituted. The real attachment and lifecycle code is compiled.
    internal static class HostSelection
    {
        internal static Type HostType(string name) => name == "QSC.QSys.windows.ComponentInfo"
            ? typeof(ComponentInfoFixture) : throw new ArgumentException(name);
    }
}

internal sealed class ComponentInfoFixture : UserControl
{
    internal const string CompleteSource = "gain.1 ( Main Level ) ( Float )\r\nmute.1 ( Main Mute ) ( Boolean )";
    private readonly TextBox txt = new() { IsReadOnly = true, Text = CompleteSource };

    internal ComponentInfoFixture()
    {
        var grid = new Grid();
        grid.Children.Add(txt);
        Content = grid;
    }

    internal string SourceText => txt.Text;
}
