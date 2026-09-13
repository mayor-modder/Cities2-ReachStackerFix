using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Simulation;
using Game.Tools;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

namespace ReachStackerFix;

internal static class WorkVehicleQueryPatch
{
    private static readonly FieldInfo? AreaQueryField = typeof(AreaLotSimulationSystem)
        .GetField("m_AreaQuery", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
    private static readonly Dictionary<AreaLotSimulationSystem, Entry> Entries = new();
    private static bool _enabled;

    private sealed class Entry
    {
        public EntityQuery Original;
        public EntityQuery Replacement;
        public bool Applied;
        public bool ConflictReported;
    }

    internal static void Enable()
    {
        if (AreaQueryField == null || AreaQueryField.FieldType != typeof(EntityQuery))
            throw new NotSupportedException("AreaLotSimulationSystem.m_AreaQuery is missing or changed.");

        foreach (var name in new[] { "OnCreate", "OnUpdate" })
        {
            var method = typeof(AreaLotSimulationSystem).GetMethod(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null, Type.EmptyTypes, null);
            if (method == null || method.ReturnType != typeof(void))
                throw new NotSupportedException($"AreaLotSimulationSystem.{name} has changed.");
        }
        _enabled = true;
    }

    private static bool IsAlive(AreaLotSimulationSystem system)
        => system.World != null && system.World.IsCreated;

    internal static void DisableAndRestore()
    {
        _enabled = false;
        foreach (var pair in Entries)
        {
            try
            {
                var entry = pair.Value;
                if (entry.Applied && IsAlive(pair.Key) &&
                    ((EntityQuery)AreaQueryField!.GetValue(pair.Key)).Equals(entry.Replacement))
                    AreaQueryField.SetValue(pair.Key, entry.Original);
            }
            catch (Exception exception)
            {
                Mod.Log.Error(exception, "Could not restore a surviving area's work-vehicle query.");
            }
        }
        Entries.Clear();
        // Both queries belong to the system. Its RequireForUpdate still holds Original.
        // Unity must dispose them when the system is destroyed, never here.
    }

    private static EntityQueryDesc ExpectedDescription() => new()
    {
        Any = new[]
        {
            ComponentType.ReadWrite<Extractor>(),
            ComponentType.ReadWrite<Storage>(),
            ComponentType.ReadWrite<CargoTransportStation>(),
        },
        None = new[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>() },
    };

    private static EntityQuery CreateQuery(AreaLotSimulationSystem system, EntityQueryDesc description)
    {
        // TryBuildDescription only accepts the known Any/None-only vanilla descriptor.
        var any = new FixedList64Bytes<ComponentType>();
        var none = new FixedList64Bytes<ComponentType>();
        foreach (var component in description.Any)
            any.Add(component);
        foreach (var component in description.None)
            none.Add(component);
        var builder = new EntityQueryBuilder(Allocator.Temp);
        try
        {
            builder.WithAny(ref any).WithNone(ref none);
            // This public overload registers dependency access and system-owned disposal.
            return system.GetEntityQuery(in builder);
        }
        finally
        {
            builder.Dispose();
        }
    }

    private static void EnsureApplied(AreaLotSimulationSystem system)
    {
        if (!_enabled)
            return;

        try
        {
            if (Entries.TryGetValue(system, out var existing))
            {
                if (existing.Applied && !existing.ConflictReported &&
                    !((EntityQuery)AreaQueryField!.GetValue(system)).Equals(existing.Replacement))
                {
                    existing.ConflictReported = true;
                    Mod.Log.Warn("Work-vehicle query was replaced after Reach Stacker Fix applied. " +
                        "Leaving the other query unchanged; protection may no longer be active.");
                }
                return;
            }

            // Retire destroyed worlds when a new system appears (for example, city reload).
            foreach (var retired in Entries.Keys.Where(key => !IsAlive(key)).ToArray())
                Entries.Remove(retired);

            var entry = new Entry();
            Entries.Add(system, entry); // Remember failures too, avoiding repeated attempts/log spam.
            var original = (EntityQuery)AreaQueryField!.GetValue(system);
            if (!QueryGuard.TryBuildDescription(original.GetEntityQueryDescs(), ExpectedDescription(),
                    ComponentType.ReadOnly<StorageTransfer>(), original.HasFilter(), out var description))
            {
                Mod.Log.Warn("Reach Stacker Fix inactive: the work-vehicle query has an unexpected " +
                    "structure or filter. The game version or another mod may have changed it.");
                return;
            }

            var replacement = CreateQuery(system, description!);
            var builtDescriptions = replacement.GetEntityQueryDescs();
            if (replacement.HasFilter() || builtDescriptions.Length != 1 ||
                !description!.Equals(builtDescriptions[0]))
                throw new NotSupportedException("The constructed work-vehicle query differs from the requested guard.");

            entry.Original = original;
            entry.Replacement = replacement;
            AreaQueryField.SetValue(system, replacement);
            entry.Applied = true;
            Mod.Log.Info($"Freight-path guard active in world '{system.World.Name}': " +
                "stations with StorageTransfer are excluded from work-vehicle processing.");
        }
        catch (Exception exception)
        {
            Mod.Log.Error(exception, "Reach Stacker Fix could not apply its query guard. Vanilla processing continues.");
        }
    }

    [HarmonyPatch(typeof(AreaLotSimulationSystem), "OnCreate")]
    private static class CreatePatch
    {
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void Postfix(AreaLotSimulationSystem __instance) => EnsureApplied(__instance);
    }

    [HarmonyPatch(typeof(AreaLotSimulationSystem), "OnUpdate")]
    private static class UpdatePatch
    {
        // The game's systems can already exist when IMod.OnLoad runs.
        [HarmonyPrefix, HarmonyPriority(Priority.Last)]
        private static void Prefix(AreaLotSimulationSystem __instance) => EnsureApplied(__instance);
    }
}
