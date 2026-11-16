using System;
using Godot;

namespace AbilitySystem;

/// <summary>
/// Live tag state of an actor. Each tag has a counter so several sources can grant the same tag.
/// Hierarchical queries are O(1): adding "A.B.C" also counts toward "A.B" and "A".
/// </summary>
public sealed class TagCountContainer
{
    private readonly int[] _explicitCounts;
    private readonly int[] _implicitCounts;

    /// <summary>Raised for the changed tag and each of its parents, with the new hierarchical count.</summary>
    public event Action<Tag, int> CountChanged;

    /// <summary>Raised when a hierarchical count goes 0 → positive (true) or positive → 0 (false).</summary>
    public event Action<Tag, bool> PresenceChanged;

    public TagCountContainer()
    {
        if (!TagRegistry.IsInitialized)
            GD.PushError("TagCountContainer created before TagRegistry was initialized.");

        _explicitCounts = new int[TagRegistry.Count];
        _implicitCounts = new int[TagRegistry.Count];
    }

    public bool Has(Tag tag) => _implicitCounts[tag.Id] > 0;

    public bool HasExact(Tag tag) => _explicitCounts[tag.Id] > 0;

    public int GetCount(Tag tag) => _implicitCounts[tag.Id];

    public int GetExactCount(Tag tag) => _explicitCounts[tag.Id];

    /// <summary>False when <paramref name="tags"/> is empty.</summary>
    public bool HasAny(TagContainer tags)
    {
        for (int i = 0; i < tags.Count; i++)
        {
            if (Has(tags[i]))
                return true;
        }

        return false;
    }

    /// <summary>True when <paramref name="tags"/> is empty.</summary>
    public bool HasAll(TagContainer tags)
    {
        for (int i = 0; i < tags.Count; i++)
        {
            if (!Has(tags[i]))
                return false;
        }

        return true;
    }

    public void Add(Tag tag, int count = 1)
    {
        if (!tag.IsValid || count <= 0)
            return;

        ApplyDelta(tag, count);
    }

    /// <summary>Removes up to <paramref name="count"/>; never goes below zero.</summary>
    public void Remove(Tag tag, int count = 1)
    {
        if (!tag.IsValid || count <= 0)
            return;

        int removable = Math.Min(count, _explicitCounts[tag.Id]);
        if (removable > 0)
            ApplyDelta(tag, -removable);
    }

    public void Add(TagContainer tags)
    {
        for (int i = 0; i < tags.Count; i++)
            Add(tags[i]);
    }

    public void Remove(TagContainer tags)
    {
        for (int i = 0; i < tags.Count; i++)
            Remove(tags[i]);
    }

    private void ApplyDelta(Tag tag, int delta)
    {
        _explicitCounts[tag.Id] += delta;

        // All counts are updated before any listener runs, so listeners always see a consistent state.
        Span<int> newCounts = stackalloc int[TagRegistry.MaxDepth];
        int depth = 0;
        for (int id = tag.Id; id != 0; id = TagRegistry.GetParentId(id))
        {
            _implicitCounts[id] += delta;
            newCounts[depth++] = _implicitCounts[id];
        }

        if (CountChanged == null && PresenceChanged == null)
            return;

        depth = 0;
        for (int id = tag.Id; id != 0; id = TagRegistry.GetParentId(id))
        {
            var changedTag = new Tag(id);
            int newCount = newCounts[depth++];
            int oldCount = newCount - delta;

            CountChanged?.Invoke(changedTag, newCount);

            if (oldCount == 0)
                PresenceChanged?.Invoke(changedTag, true);
            else if (newCount == 0)
                PresenceChanged?.Invoke(changedTag, false);
        }
    }
}
