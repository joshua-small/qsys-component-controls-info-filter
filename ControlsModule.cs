using QsysMods.Core;
namespace Designer.Modules.ComponentControlsInfoFilter;
public sealed class ControlsModule : ModModule
{
    public ControlsModule() => Mods.Register("component-controls-info-filter", _ => ControlsInfoSearch.Install((kind, detail) => Host.Receipt("component-controls-info-filter", kind, detail)));
}
