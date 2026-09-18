# Reach Stacker Fix

Prevents the area work-vehicle system from treating a cargo station's freight route as a reach-stacker work route. This targets airport-owned reach stackers spawning at a distant cargo rail terminal and driving across the city to the airport.

Version **0.1.0** has passed its first in-game airport test on Cities: Skylines II 1.6.0f1. After existing airport-owned reach stackers were removed, eight normal freight vehicles replaced them and the stackers did not return during the reported observation period. The runtime log confirmed the guard was active. Save/reload, uninstall and normal port-work checks remain pending.

## Compatibility

Package metadata targets Cities: Skylines II **`1.6.*`**. The mod was implemented and first playtested against **1.6.0f1**, and compatibility with **1.6.2f1** was checked on 2026-09-18. The three relevant game systems were unchanged from the original diagnosis, the post-patch runtime log confirmed successful guard activation with no mod warnings or errors, and all 20 descriptor regression tests passed against the updated Unity assemblies. The faulty vanilla code path remains present, so the fix is still needed on 1.6.2f1.

This check did not include a fresh reproduction with the mod disabled or a new save/reload, uninstall or normal port-work playtest. Other game versions have not been verified; the declared version range is not a guarantee of compatibility with future patches. Uses Harmony 2.2.2 from the Cities2-MCP C# template.

The mod checks the vanilla work query's component sets, access modes, options and filters before changing it. An unexpected query produces a warning and remains unchanged. Mods that replace the same query may conflict; if another replacement is observed later, this mod logs it and leaves that query alone.

No settings or UI are required. No custom components are saved, no vehicle prefabs are changed, and existing reach stackers are not deleted. Existing erroneous trips may finish normally or can be removed manually before testing new spawns.

Normal yard work remains eligible when no freight-transfer marker is present. While a cargo station has an active transfer, its yard work and work-vehicle bookkeeping can pause. The reverse shared-path problem, freight processing consuming an existing yard-work path, is outside this focused fix.

## Build

Requires the configured official CS2 modding toolchain and .NET SDK 8. The official `Mod.props` selects .NET Framework 4.8 on the development machine. Assemblies are referenced from `$(ManagedPath)` and are not bundled with the mod.

Run from this directory:

```powershell
dotnet run --project tests/ReachStackerFix.Tests.csproj -c Release
dotnet build reach-stacker-fix.csproj -c Release
```

The tests use real Unity query descriptors with symbolic type indexes, so they run without starting Unity. They check descriptor transformation and compatibility rejection; they do not simulate native ECS queries or Harmony activation. A test machine with a different installation path can pass `-p:Cities2ManagedPath="<game managed directory>"`.

The build retains the official Entities/Burst/versioning postprocessor. Its deployment target is redirected to `artifacts\playtest\ReachStackerFix` inside this project. A normal build does not install to the live game's Mods folder. Keep the complete staged folder, including `0Harmony.dll` and generated native companion files.

## Local playtest

1. Close Cities: Skylines II before installing or replacing files. Copy the staged `ReachStackerFix` folder to `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\ReachStackerFix`.
2. Launch the game and load a separate test save of the affected city. Confirm the expected playset and local mod are active. In `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\ReachStackerFix.log`, look for `Managed lifecycle hooks installed` and, after simulation resumes, `Freight-path guard active`. A missing activation log, an `inactive` warning, or a load/apply error means prevention is not confirmed. Check `Logs\Modding.log` and `Player.log` if the mod is not discovered or loaded.
3. Resume the city through several cargo cycles with the original airport import route active. Watch the industrial-island rail cargo terminal that previously spawned the airport-owned stackers. The success signal is continued normal cargo transfers and truck/van traffic with no new airport-owned reach stackers taking that route. Existing vehicles alone do not demonstrate failure; identify newly spawned trips.
4. Check a working cargo port that normally uses reach stackers. Its legitimate yard vehicles should still work outside active freight-transfer periods. Verify ordinary cargo trucks continue delivering, station storage changes, and extractor/resource production continues.
5. Save to the test slot, return to the menu and reload it. Repeat the activation/log and traffic checks to cover a new system/world lifecycle. Check that no new exceptions appear in `ReachStackerFix.log` or `Player.log`.
6. To uninstall, close the game and remove only `Mods\ReachStackerFix`, then relaunch the test save. Confirm the mod no longer loads and the city opens. The mod adds no serialized data; the original game bug can recur without it.

Report problems through [GitHub issues](https://github.com/mayor-modder/Cities2-ReachStackerFix/issues). Include the game version, active mod list, relevant log excerpts, and a screenshot identifying the vehicle's owner, origin and destination. There is no mod UI to inspect in the UI debugger.

## Implementation evidence

See [design](docs/design.md) for the diagnosed path conflict and query ownership, and [validation](docs/validation.md) for build/test evidence and pending gameplay checks.

Lifecycle and toolchain context: Cities2-MCP wiki dataset, [Systems](https://cs2.paradoxwikis.com/Systems) and [Modding toolchain](https://cs2.paradoxwikis.com/Modding_Toolchain). Current API signatures were checked against the installed game assemblies.
