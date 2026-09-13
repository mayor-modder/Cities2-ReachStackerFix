using ReachStackerFix;
using Unity.Entities;

// Real Unity descriptors and component values; fabricated indices avoid the native TypeManager.
// These tests check descriptor transformation. They do not simulate EntityQuery execution.
var checks = new (string Name, Action Test)[]
{
    ("Vanilla descriptor retains extractor, storage, and station alternatives", PreservesAlternatives),
    ("Transfer exclusion retains existing temporary and deleted exclusions", AddsTransferExclusion),
    ("Original arrays remain unchanged and do not alias replacement", OriginalIsImmutable),
    ("Equivalent reordered component arrays are accepted", ReorderedEquivalentIsAccepted),
    ("Additional Any entry is rejected", () => RejectMutation(d => d.Any = d.Any.Append(Rw(20)).ToArray())),
    ("Missing Any entry is rejected", () => RejectMutation(d => d.Any = d.Any.Take(2).ToArray())),
    ("Changed All set is rejected", () => RejectMutation(d => d.All = new[] { Ro(20) })),
    ("Changed None set is rejected", () => RejectMutation(d => d.None = new[] { Ro(20) })),
    ("Changed Disabled set is rejected", () => RejectMutation(d => d.Disabled = new[] { Ro(20) })),
    ("Changed Absent set is rejected", () => RejectMutation(d => d.Absent = new[] { Ro(20) })),
    ("Changed Present set is rejected", () => RejectMutation(d => d.Present = new[] { Ro(20) })),
    ("Changed Any access mode is rejected", () => RejectMutation(d => d.Any[0] = Ro(1))),
    ("Changed None access mode is rejected", () => RejectMutation(d => d.None[0] = Rw(4))),
    ("Changed query options are rejected", () => RejectMutation(d => d.Options = EntityQueryOptions.IncludeDisabledEntities)),
    ("Active query filters are rejected without a replacement", FilterIsRejected),
    ("Zero query descriptions are rejected", () => RejectDescriptions(Array.Empty<EntityQueryDesc>())),
    ("Multiple query descriptions are rejected", () => RejectDescriptions(new[] { Vanilla(), Vanilla() })),
    ("An unrelated query is rejected unchanged", UnrelatedQueryIsUnchanged),
    ("An already guarded query is rejected without a duplicate exclusion", AlreadyGuardedIsRejected),
    ("Input aliases expected descriptor without mutating either", SameDescriptionIsAccepted),
};
var failures = 0;
foreach (var (name, test) in checks)
{
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
Console.WriteLine($"{checks.Length - failures}/{checks.Length} tests passed.");
return failures == 0 ? 0 : 1;

static ComponentType Rw(int index) => new() { TypeIndex = new TypeIndex { Value = index }, AccessModeType = ComponentType.AccessMode.ReadWrite };
static ComponentType Ro(int index) => new() { TypeIndex = new TypeIndex { Value = index }, AccessModeType = ComponentType.AccessMode.ReadOnly };
static ComponentType Transfer() => Ro(6);
static EntityQueryDesc Vanilla() => new()
{
    Any = new[] { Rw(1), Rw(2), Rw(3) }, // Extractor, Storage, CargoTransportStation
    None = new[] { Ro(4), Ro(5) }, // Temp, Deleted
};
static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
static EntityQueryDesc Guard(EntityQueryDesc actual, EntityQueryDesc? expected = null)
{
    Assert(QueryGuard.TryBuildDescription(new[] { actual }, expected ?? Vanilla(), Transfer(), false, out var result), "Expected an eligible descriptor.");
    Assert(result != null, "Success must provide a replacement descriptor.");
    return result!;
}
static void PreservesAlternatives()
{
    var original = Vanilla();
    var result = Guard(original);
    Assert(result.Any.SequenceEqual(original.Any), "The three original work-area alternatives must be preserved with access modes.");
    Assert(result.All.Length == 0, "Transfer must not become an All requirement.");
    Assert(result.Options == original.Options && result.Disabled.Length == 0 && result.Absent.Length == 0 && result.Present.Length == 0, "Other query constraints changed.");
}
static void AddsTransferExclusion()
{
    var result = Guard(Vanilla());
    Assert(result.None.SequenceEqual(new[] { Ro(4), Ro(5), Transfer() }), "Expected original exclusions followed by exactly one transfer exclusion.");
    Assert(result.None.All(c => c.AccessModeType != ComponentType.AccessMode.Exclude), "EntityQueryDesc.None needs regular component access modes, not Exclude values.");
}
static void OriginalIsImmutable()
{
    var original = Vanilla();
    var snapshot = Vanilla();
    var result = Guard(original);
    Assert(original.Equals(snapshot), "Building the guard mutated the original description.");
    Assert(!ReferenceEquals(original, result), "Replacement aliases original object.");
    Assert(!ReferenceEquals(original.Any, result.Any) && !ReferenceEquals(original.None, result.None), "Replacement aliases a mutable source array.");
    result.Any[0] = Ro(30);
    result.None[0] = Ro(31);
    Assert(original.Equals(snapshot), "Mutating replacement arrays changed original query.");
    original.Any[1] = Ro(32);
    Assert(result.Any[1].Equals(Rw(2)), "Mutating original array changed replacement query.");
}
static void ReorderedEquivalentIsAccepted()
{
    var original = Vanilla();
    Array.Reverse(original.Any);
    Array.Reverse(original.None);
    var result = Guard(original);
    Assert(result.Any.SequenceEqual(original.Any), "Accepted query order was not retained.");
    Assert(result.None.Take(original.None.Length).SequenceEqual(original.None), "Original exclusion order was not retained.");
}
static void RejectMutation(Action<EntityQueryDesc> mutate)
{
    var actual = Vanilla();
    mutate(actual);
    RejectDescriptions(new[] { actual });
}
static void RejectDescriptions(EntityQueryDesc[] actual)
{
    Assert(!QueryGuard.TryBuildDescription(actual, Vanilla(), Transfer(), false, out var result), "Unexpectedly accepted incompatible query shape.");
    Assert(result == null, "Rejected query must not provide a replacement.");
}
static void FilterIsRejected()
{
    Assert(!QueryGuard.TryBuildDescription(new[] { Vanilla() }, Vanilla(), Transfer(), true, out var result), "Filtered queries must be rejected.");
    Assert(result == null, "Filtered query returned replacement.");
}
static void UnrelatedQueryIsUnchanged()
{
    var actual = new EntityQueryDesc { All = new[] { Rw(20) }, None = new[] { Ro(5) } };
    var all = actual.All.ToArray();
    var none = actual.None.ToArray();
    RejectDescriptions(new[] { actual });
    Assert(actual.All.SequenceEqual(all) && actual.None.SequenceEqual(none), "Rejected unrelated query was modified.");
}
static void AlreadyGuardedIsRejected()
{
    var guarded = Guard(Vanilla());
    var none = guarded.None.ToArray();
    RejectDescriptions(new[] { guarded });
    Assert(guarded.None.SequenceEqual(none), "Second call changed existing guard.");
}
static void SameDescriptionIsAccepted()
{
    var source = Vanilla();
    var result = Guard(source, source);
    Assert(source.Equals(Vanilla()), "Using same object for actual and expected mutated source.");
    Assert(result.None.Length == 3, "Guard exclusion missing.");
}
