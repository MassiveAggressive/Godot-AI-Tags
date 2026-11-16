using System;
using System.Collections.Generic;
using Godot;

namespace AbilitySystem;

/// <summary>
/// Inspector-editable tag set. Initial tags are picked from a dropdown fed by Tags.tres and resolved to ids on first use.
/// A .tres instance is shared by everyone who loads it: treat exported containers as read-only data and keep live actor
/// state in <see cref="TagCountContainer"/>. Iterate with <see cref="Count"/> and the indexer to avoid enumerator allocation.
/// </summary>
[Tool]
[GlobalClass]
public partial class TagContainer : Resource
{
    private string[] _initialTags = Array.Empty<string>();
    private List<Tag> _tags;

    // Godot Resource + [Export] are required for inspector editing; [Tool] lets _ValidateProperty fill the dropdown in the editor.
    [Export]
    public string[] InitialTags
    {
        get => _initialTags;
        set
        {
            _initialTags = value ?? Array.Empty<string>();
            _tags = null;
        }
    }

    public int Count => Tags.Count;

    public Tag this[int index] => Tags[index];

    private List<Tag> Tags
    {
        get
        {
            if (_tags == null)
                ResolveInitialTags();

            return _tags;
        }
    }

    public bool Add(Tag tag)
    {
        if (!tag.IsValid || HasExact(tag))
            return false;

        Tags.Add(tag);
        return true;
    }

    public bool Remove(Tag tag) => Tags.Remove(tag);

    public void Clear() => Tags.Clear();

    /// <summary>True if any tag in this container is <paramref name="tag"/> or one of its children.</summary>
    public bool Has(Tag tag)
    {
        List<Tag> tags = Tags;
        for (int i = 0; i < tags.Count; i++)
        {
            if (TagRegistry.Matches(tags[i], tag))
                return true;
        }

        return false;
    }

    public bool HasExact(Tag tag)
    {
        List<Tag> tags = Tags;
        for (int i = 0; i < tags.Count; i++)
        {
            if (tags[i].Id == tag.Id)
                return true;
        }

        return false;
    }

    /// <summary>False when <paramref name="other"/> is empty.</summary>
    public bool HasAny(TagContainer other)
    {
        for (int i = 0; i < other.Count; i++)
        {
            if (Has(other[i]))
                return true;
        }

        return false;
    }

    /// <summary>True when <paramref name="other"/> is empty.</summary>
    public bool HasAll(TagContainer other)
    {
        for (int i = 0; i < other.Count; i++)
        {
            if (!Has(other[i]))
                return false;
        }

        return true;
    }

    // Godot API: the property info arrives as a Godot Dictionary.
    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        if (!Engine.IsEditorHint() || property["name"].AsString() != nameof(InitialTags))
            return;

        property["hint"] = (int)PropertyHint.TypeString;
        property["hint_string"] = $"{(int)Variant.Type.String}/{(int)PropertyHint.Enum}:{BuildTagOptions()}";
    }

    /// <summary>Comma separated, sorted tag names from Tags.tres including parents. Editor only.</summary>
    internal static string BuildTagOptions()
    {
        var tagList = GD.Load<TagList>(TagList.DefaultPath);
        if (tagList == null)
            return string.Empty;

        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string tagName in tagList.Tags)
        {
            if (!TagRegistry.IsValidName(tagName))
                continue;

            // Parents are selectable too, matching what TagRegistry registers.
            for (int dot = tagName.IndexOf('.'); dot > 0; dot = tagName.IndexOf('.', dot + 1))
                names.Add(tagName.Substring(0, dot));

            names.Add(tagName);
        }

        return string.Join(",", names);
    }

    private void ResolveInitialTags()
    {
        _tags = new List<Tag>(_initialTags.Length);
        foreach (string tagName in _initialTags)
        {
            // New array elements start empty in the inspector; skip them instead of reporting an error.
            if (!string.IsNullOrEmpty(tagName))
                Add(TagRegistry.Request(tagName));
        }
    }
}
