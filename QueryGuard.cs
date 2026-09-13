using System;
using Unity.Entities;

namespace ReachStackerFix;

internal static class QueryGuard
{
    // Kept independent of TypeManager so the descriptor policy can be tested outside Unity.
    internal static bool TryBuildDescription(
        EntityQueryDesc[] actual,
        EntityQueryDesc expected,
        ComponentType transfer,
        bool hasFilter,
        out EntityQueryDesc? replacement)
    {
        replacement = null;
        if (hasFilter || actual.Length != 1 || !expected.Equals(actual[0]))
            return false;

        var source = actual[0];
        var excluded = new ComponentType[source.None.Length + 1];
        Array.Copy(source.None, excluded, source.None.Length);
        excluded[excluded.Length - 1] = transfer;
        replacement = new EntityQueryDesc
        {
            Any = (ComponentType[])source.Any.Clone(),
            All = (ComponentType[])source.All.Clone(),
            None = excluded,
            Disabled = (ComponentType[])source.Disabled.Clone(),
            Absent = (ComponentType[])source.Absent.Clone(),
            Present = (ComponentType[])source.Present.Clone(),
            Options = source.Options,
        };
        return true;
    }
}
