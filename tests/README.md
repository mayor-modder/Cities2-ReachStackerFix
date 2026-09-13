# Query guard tests

Run `dotnet run --project tests/ReachStackerFix.Tests.csproj` with the .NET 8 SDK installed. The project references the local game's Unity assemblies and compiles the production `QueryGuard.cs` directly; it does not redistribute game assemblies.

For another installation path, pass `-p:Cities2ManagedPath="D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed"`.

The tests use the actual `Unity.Entities.EntityQueryDesc`, `ComponentType`, and `TypeIndex` types. Fabricated type indices let descriptor matching and transformation run without Unity's native type registry. The tests cover the exclusion's placement, preservation of the original query, compatibility rejection, filters, access modes, and independent replacement arrays.

These are descriptor tests. They do not instantiate a Unity world, execute an entity query, patch a running system, or establish in-game behavior. `ComponentType.ReadOnly<T>()` requires Unity's native type initialization, and `GetEntityQueryDescs()` requires a live query. Those integration boundaries need in-game verification.
