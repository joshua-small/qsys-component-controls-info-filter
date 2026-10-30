# Component Controls Info Filter

Filters Component Controls Info by control name and type without changing the source controls. This passive mod operates inside its native editor; it has no empty operation window or preferences page.

Requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) 0.1.x, Q-SYS Mod Manager 0.8+, and the exact supported Designer 10.5 build. Use the manager migration to avoid duplicate hooks. Better Shortcuts is optional.

Clone Core beside this repository. Build Designer.Modules.ComponentControlsInfoFilter.csproj with .NET 10.0.112 and local DesignerDir references. Vendor assemblies remain local. The feature is extracted from Schematic Tools commit 13c52e6 with shared host access routed through Core; the split passes compilation and isolated-host startup. No new hardware support is claimed.

[Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).

## Portable regression tests

From this repository's root, with the .NET SDK selected by `global.json` installed:

```sh
dotnet run --project tests/Portable/ComponentControlsInfoFilter.PortableTests.csproj --configuration Release
```

The runner compiles the same `ControlsInfoFilter.cs` used by the mod. It uses only the .NET SDK, with no package feeds, WPF, Q-SYS Mods Core, Designer installation, or vendor assemblies. A failed case returns a nonzero exit code. The portable test workflow runs this command on pull requests and changes to `main`.

Synthetic fixtures cover name/label/type substrings, all-term case-insensitive matching, whitespace and multiline queries, literal punctuation, no matches, counts, ordering, duplicate rows, newline handling, message passthrough, successive inputs, and a 2,000-row list. The row-shape check preserves existing behavior; it is not a validation of Designer's text format.

These checks cover text transformation only. Building the full mod still requires the dependencies above. WPF event wiring, focus, keyboard interaction, selection/copy, real component changes, and preservation of the native source and design state remain separate [application acceptance work](https://github.com/joshua-small/qsys-component-controls-info-filter/issues/2).

## Floating window behavior

The filter gives its current floating Component Controls Info host the title `Component Controls Info`, a normal single-border window style, and taskbar visibility. It refreshes these properties when the inspector loads or changes presentation source, including after docking and refloating. The main Designer window is excluded by object identity. Pending refresh work and presentation-source observation are removed on unload, then restored on reload; the existing filter and query are retained.

The WPF regression runner exercises the production window helper and attachment code with synthetic inspector contents. Property checks use unshown windows; lifecycle checks create real off-screen windows without requesting focus, move the retained inspector between hosts, and drain the WPF dispatcher. On Windows with the .NET 10 SDK and Windows Desktop runtime:

```sh
dotnet run --project tests/Window/ComponentControlsInfoFilter.WindowTests.csproj --configuration Release
```

It checks the floating properties, missing-host handling, repeated application, main-window exclusion across every window style and taskbar setting, docked-first opening, recreated/reused hosts, close/reopen, rapid reparenting, unloaded cleanup and retained filter/source state. Only the Core host-type lookup is substituted; it needs neither Designer nor vendor assemblies. Building this runner on another OS does not run its checks. Version 0.1.2 was checked on Designer 10.5.0 with Mods Core 0.1.11: two dock/float cycles, closing and reopening the floating inspector, reopening an existing inspector docked and then floating it, Ctrl+F, filtering, copying and Clear. Active queries followed two real components with distinct 22- and 15-control lists without stale rows; direct readback confirmed that filtering and clearing preserved the complete native source text. The same inspector survived docking and refloating in light and dark styles, with readable query, count and output and working focus/input. Main-window properties, models, dirty state and history were preserved, and the original preferences were restored after normal exit.

First construction of a brand-new inspector while docked is covered by the standalone runner only. Actual shell taskbar-button interaction and Designer 10.5.1 remain unvalidated. These checks establish no connected-Core or hardware support.
