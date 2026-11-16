using System;
using System.Collections.Generic;
using Godot;

namespace AbilitySystem;

/// <summary>
/// Maps tag names to ids and stores the parent chain. Initialized once by <see cref="TagLoader"/>, then locked.
/// </summary>
public static class TagRegistry
{
    // Index 0 is Tag.None.
    private static string[] _names = { string.Empty };
    private static int[] _parentIds = { 0 };
    private static readonly Dictionary<string, int> _idsByName = new(StringComparer.Ordinal);

    public static bool IsInitialized { get; private set; }

    /// <summary>Number of ids including <see cref="Tag.None"/>; arrays of this size can be indexed by <see cref="Tag.Id"/>.</summary>
    public static int Count => _names.Length;

    /// <summary>Level count of the deepest tag ("A.B.C" is 3).</summary>
    public static int MaxDepth { get; private set; }

    public static void Initialize(string[] tagNames)
    {
        if (IsInitialized)
        {
            GD.PushError("TagRegistry is already initialized.");
            return;
        }

        var names = new List<string> { string.Empty };
        var parentIds = new List<int> { 0 };

        if (tagNames != null)
        {
            foreach (string tagName in tagNames)
                RegisterWithParents(tagName, names, parentIds);
        }

        _names = names.ToArray();
        _parentIds = parentIds.ToArray();
        MaxDepth = CalculateMaxDepth();
        IsInitialized = true;
    }

    /// <summary>Resolves a tag by full name. Call at load time, not every frame.</summary>
    public static Tag Request(string name)
    {
        if (!IsInitialized)
        {
            GD.PushError("TagRegistry is not initialized. Is the TagLoader autoload registered?");
            return Tag.None;
        }

        if (name != null && _idsByName.TryGetValue(name, out int id))
            return new Tag(id);

        GD.PushError($"Tag not found: '{name}'.");
        return Tag.None;
    }

    /// <summary>True if <paramref name="tag"/> is <paramref name="parent"/> or one of its children.</summary>
    public static bool Matches(Tag tag, Tag parent)
    {
        int parentId = parent.Id;
        if (parentId == 0)
            return false;

        for (int id = tag.Id; id != 0; id = _parentIds[id])
        {
            if (id == parentId)
                return true;
        }

        return false;
    }

    public static string GetName(Tag tag) => tag.IsValid ? _names[tag.Id] : "None";

    internal static int GetParentId(int id) => _parentIds[id];

    private static void RegisterWithParents(string fullName, List<string> names, List<int> parentIds)
    {
        if (!IsValidName(fullName))
        {
            GD.PushError($"Invalid tag name: '{fullName}'.");
            return;
        }

        int parentId = 0;
        int dotIndex = -1;
        do
        {
            dotIndex = fullName.IndexOf('.', dotIndex + 1);
            string name = dotIndex < 0 ? fullName : fullName.Substring(0, dotIndex);

            if (!_idsByName.TryGetValue(name, out int id))
            {
                id = names.Count;
                names.Add(name);
                parentIds.Add(parentId);
                _idsByName.Add(name, id);
            }

            parentId = id;
        }
        while (dotIndex >= 0);
    }

    internal static bool IsValidName(string name)
    {
        if (string.IsNullOrEmpty(name) || name[0] == '.' || name[^1] == '.')
            return false;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            // ',' and ':' would break the editor dropdown hint string.
            if (char.IsWhiteSpace(c) || c == ',' || c == ':')
                return false;

            // i > 0 is guaranteed here because name[0] is not '.'.
            if (c == '.' && name[i - 1] == '.')
                return false;
        }

        return true;
    }

    private static int CalculateMaxDepth()
    {
        int maxDepth = 0;
        for (int i = 1; i < _parentIds.Length; i++)
        {
            int depth = 0;
            for (int id = i; id != 0; id = _parentIds[id])
                depth++;

            if (depth > maxDepth)
                maxDepth = depth;
        }

        return maxDepth;
    }
}
