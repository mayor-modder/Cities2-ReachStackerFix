# Reach Stacker Fix design

## Problem and scope

On Cities: Skylines II 1.6.0f1, an airport-owned reach stacker can spawn at a remote cargo railway terminal and drive the terminal-to-airport freight route. The area work-vehicle job consumes `PathInformation` without distinguishing work requests from freight requests on cargo stations. Freight requests carry `Game.Companies.StorageTransfer` on the same entity.

Prevent that wrong-spawn direction by excluding active storage transfers from `AreaLotSimulationSystem.m_AreaQuery`. Preserve its existing `Any` components (`Extractor`, `Storage`, `CargoTransportStation`) and `None` components (`Temp`, `Deleted`), including access modes. Resource extraction uses a separate query. Ordinary freight processing remains in its original systems. Yard work and work-vehicle bookkeeping can temporarily pause while a station has an active storage transfer.

## Implementation

Use managed Harmony hooks on `OnCreate` and `OnUpdate`. The latter also handles systems created before mod load. Validate the field, method signatures, query descriptors, options and filters before replacing the query. Create one replacement per system through `ComponentSystemBase.GetEntityQuery(in EntityQueryBuilder)`, preserving Unity's dependency registration and query ownership. Keep the original query alive because `RequireForUpdate` still references it. Do not patch Burst job bodies.

Do not overwrite a query subsequently replaced by another mod. Restore the original on mod disposal only while the field still contains this mod's replacement. Unity owns and disposes both queries. Do not create serialized components, change assets or economy values, or delete existing vehicles.

This is not full arbitration of shared path components: the opposite collision, freight processing consuming a legitimate yard-work path, is outside this fix. A second query exclusion alone would leave deferred-command races and is not included.

## Validation

Test the actual managed Unity query descriptors without starting Unity: accept reordered equivalent descriptors, reject unexpected query structure/access/options/filters, exclude transfers, retain ordinary work eligibility, and leave the original descriptor unchanged. Compile the lifecycle adapter against installed game assemblies and run the official mod postprocessor. Independent review covers query ownership, scheduling, unload and world replacement. Native query execution, Harmony activation and actual traffic behavior require the local playtest described in the README.

## Evidence

Inspected installed `Game.dll` SHA-256: `721e7e17bf74299aa2b988c1bd07e90874bb8bc72d263229500c4bf639e7e4ee`. `AreaLotSimulationSystem.OnUpdate` schedules `ManageVehiclesJob` directly with `m_AreaQuery`; extractor jobs use `m_ExtractorQuery`. `StorageTransferSystem` permits road/cargo paths and reverses import endpoints. The stock international airport has work multiplier zero and eight freight transports; the misplaced stacker branch bypasses the work multiplier gate.

API lifecycle context: [Systems](https://cs2.paradoxwikis.com/Systems) and [Modding toolchain](https://cs2.paradoxwikis.com/Modding_Toolchain), Cities2-MCP wiki dataset. Installed assemblies are the authority for the signatures used here.
