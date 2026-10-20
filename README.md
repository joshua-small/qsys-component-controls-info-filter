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
