using System;
using System.Collections.Generic;
using Godot;

namespace Tags;

/// <summary>
/// Inspector-editable tag set. Initial tags are picked from a dropdown fed by Tags.tres and resolved to ids on first use.
/// A .tres instance is shared by everyone who loads it: treat exported containers as read-only data and keep live actor
/// state in <see cref="TagCountContainer"/>. Iterate with <see cref="Count"/> and the indexer to avoid enumerator allocation.
/// </summary>
[Tool]
[GlobalClass]
public partial class TagContainer : Resource
{
    /// <summary>
    /// Marks a single tag name string field for the editor's tag tree picker: set it as the field's
    /// <c>hint_string</c> (with <c>PropertyHint.None</c>) in <c>_ValidateProperty</c>. Without the Tags plugin the
    /// field is a plain text box.
    /// </summary>
    public const string TagNameHint = "tags:tag_name";

    // Editor only: the tag list and the names built from it (see CollectTagNames).
    private static TagList _tagList;
    private static string[] _namesSource;
    private static SortedSet<string> _names;
    private static IReadOnlySet<string> _optionsSource;
    private static string _options = string.Empty;

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

    /// <summary>
    /// Comma separated, sorted tag names from Tags.tres including parents, for an editor dropdown
    /// (<c>PropertyHint.Enum</c>) on a single tag name. Editor only; cached until the tag list changes.
    /// </summary>
    public static string BuildTagOptions()
    {
        IReadOnlySet<string> names = CollectTagNames();
        if (!ReferenceEquals(names, _optionsSource))
        {
            _options = string.Join(",", names);
            _optionsSource = names;
        }

        return _options;
    }

    /// <summary>
    /// Sorted tag names from Tags.tres including parents (what TagRegistry registers). Editor only. Cached: rebuilt
    /// when the list's tag array is replaced (a tag added) or the list file changes.
    /// </summary>
    internal static IReadOnlySet<string> CollectTagNames()
    {
        // _ValidateProperty calls this for every tag field on every inspector refresh. Loading the list each time
        // made a new C# wrapper per call; the GC finalizing those while Godot swapped their handles crashed the
        // editor (FATAL gchandle.is_released). One kept reference avoids that and the repeated work.
        string path = TagList.GetListPath();
        if (_tagList == null || !GodotObject.IsInstanceValid(_tagList) || _tagList.ResourcePath != path)
        {
            _tagList = ResourceLoader.Exists(path) ? ResourceLoader.Load(path) as TagList : null;
            _namesSource = null;
        }

        string[] tags = _tagList?.Tags;
        if (_names != null && ReferenceEquals(tags, _namesSource))
            return _names;

        _names = BuildNames(tags);
        _namesSource = tags;
        return _names;
    }

    private static SortedSet<string> BuildNames(string[] tags)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        if (tags == null)
            return names;

        foreach (string tagName in tags)
        {
            if (!TagRegistry.IsValidName(tagName))
                continue;

            // Parents are selectable too, matching what TagRegistry registers.
            for (int dot = tagName.IndexOf('.'); dot > 0; dot = tagName.IndexOf('.', dot + 1))
                names.Add(tagName.Substring(0, dot));

            names.Add(tagName);
        }

        return names;
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
