using System;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;

namespace ReachStackerFix;

public sealed class Mod : IMod
{
    internal const string HarmonyId = "ReachStackerFix.patches";
    public static readonly ILog Log = LogManager.GetLogger(nameof(ReachStackerFix)).SetShowsErrorsInUI(false);
    private Harmony? _harmony;

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info($"Loading Reach Stacker Fix {typeof(Mod).Assembly.GetName().Version}.");

        if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
        {
            Log.Info($"Current mod asset at {asset.path}");
        }

        try
        {
            WorkVehicleQueryPatch.Enable();
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(typeof(Mod).Assembly);
            Log.Info("Managed lifecycle hooks installed; the guard applies before the next area work update.");
        }
        catch (Exception exception)
        {
            WorkVehicleQueryPatch.DisableAndRestore();
            _harmony?.UnpatchAll(HarmonyId);
            _harmony = null;
            Log.Error(exception, "Reach Stacker Fix could not load. No query guard is active.");
        }
    }

    public void OnDispose()
    {
        WorkVehicleQueryPatch.DisableAndRestore();
        _harmony?.UnpatchAll(HarmonyId);
        _harmony = null;
        Log.Info("Reach Stacker Fix unloaded; surviving queries restored where still owned by this mod.");
    }
}
