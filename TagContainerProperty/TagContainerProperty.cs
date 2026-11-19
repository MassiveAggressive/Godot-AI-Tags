#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Tags;

/// <summary>
/// Inspector editor of a tag set, next to the field's name: the selected tags as a list with remove buttons; a click
/// anywhere else on it opens the tag tree (<see cref="TagTreePopup"/>) right under the field. Edits either a <see cref="TagContainer"/> field of
/// another resource (<see cref="EditsResource"/>) or the <see cref="TagContainer.InitialTags"/> of a container itself.
/// <para>
/// A container field may be empty: picking the first tag creates a new container in it, removing the last one empties
/// the field again. Each change is written to the field, so undo and redo work. A field pointing to a shared .tres
/// container edits that file in place and saves it (no undo). Tags no longer in Tags.tres are shown in the warning
/// color and can be removed.
/// </para>
/// </summary>
[Tool]
public partial class TagContainerProperty : EditorProperty
{
    private readonly VBoxContainer _box;
    private readonly VBoxContainer _list;
    private readonly Label _shared;
    private readonly TagTreePopup _popup;

    public TagContainerProperty()
    {
        // Next to the field's name, like other properties. The whole area is the button: a click anywhere on it
        // (outside the remove buttons) opens the tag tree.
        _box = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            TooltipText = "Click to pick tags",
        };
        _box.GuiInput += OnBoxInput;

        _list = new VBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        _box.AddChild(_list);

        _shared = new Label { Visible = false, ClipText = true };
        _box.AddChild(_shared);

        AddChild(_box);

        _popup = new TagTreePopup();
        _popup.SelectionChanged += Write;
        AddChild(_popup);
    }

    /// <summary>True: the edited property is a <see cref="TagContainer"/> field (may be empty). False: a string array of tag names.</summary>
    public bool EditsResource { get; set; }

    public override void _UpdateProperty()
    {
        foreach (Node child in _list.GetChildren())
        {
            _list.RemoveChild(child);
            child.QueueFree();
        }

        bool readOnly = IsReadOnly();
        _box.MouseDefaultCursorShape = readOnly ? CursorShape.Arrow : CursorShape.PointingHand;

        TagContainer container = EditsResource ? ReadContainer() : null;
        _shared.Visible = container != null && IsShared(container);
        if (_shared.Visible)
        {
            _shared.Text = $"Shared: {container.ResourcePath}";
            _shared.TooltipText = "This container is its own file, used by other resources too; edits change the file.";
            _shared.AddThemeColorOverride("font_color", GetThemeColor("font_disabled_color", "Editor"));
        }

        string[] tags = ReadTags();
        if (tags.Length == 0)
        {
            var none = new Label { Text = "(no tags, click to add)" };
            none.AddThemeColorOverride("font_color", GetThemeColor("font_disabled_color", "Editor"));
            _list.AddChild(none);
            return;
        }

        IReadOnlySet<string> known = TagContainer.CollectTagNames();
        Color warning = GetThemeColor("warning_color", "Editor");
        foreach (string name in tags)
        {
            if (string.IsNullOrEmpty(name))
                continue;

            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Pass };
            // Pass: shows its tooltip, and the click still reaches the box (opens the tree).
            var label = new Label { Text = name, SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true, TooltipText = name, MouseFilter = MouseFilterEnum.Pass };
            if (!known.Contains(name))
            {
                label.AddThemeColorOverride("font_color", warning);
                label.TooltipText = $"'{name}' is not in the tag list.";
            }

            row.AddChild(label);

            var remove = new Button { Flat = true, TooltipText = "Remove", Disabled = readOnly };
            Texture2D icon = HasThemeIcon("Remove", "EditorIcons") ? GetThemeIcon("Remove", "EditorIcons") : null;
            if (icon != null)
                remove.Icon = icon;
            else
                remove.Text = "×";

            string removed = name;
            remove.Pressed += () => Write(Without(ReadTags(), removed));
            row.AddChild(remove);

            _list.AddChild(row);
        }
    }

    private void OnBoxInput(InputEvent @event)
    {
        if (IsReadOnly() || @event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            return;

        _box.AcceptEvent();
        OpenPicker();
    }

    private void OpenPicker()
    {
        var anchor = new Rect2I((Vector2I)GetScreenPosition(), (Vector2I)Size);
        _popup.Open(ReadTags(), multi: true, anchor);
    }

    private TagContainer ReadContainer() => GetEditedObject().Get(GetEditedProperty()).AsGodotObject() as TagContainer;

    private string[] ReadTags()
    {
        string[] tags = EditsResource
            ? ReadContainer()?.InitialTags
            : GetEditedObject().Get(GetEditedProperty()).AsStringArray();

        return tags ?? Array.Empty<string>();
    }

    /// <summary>Its own .tres file (shared), not a container embedded in the edited resource.</summary>
    private static bool IsShared(Resource resource) =>
        !string.IsNullOrEmpty(resource.ResourcePath) && !resource.ResourcePath.Contains("::", StringComparison.Ordinal);

    private void Write(string[] tags)
    {
        tags = Clean(tags);
        if (!EditsResource)
        {
            EmitChanged(GetEditedProperty(), tags);
            return;
        }

        // The last tag gone: the field is empty again, as if no container was ever made.
        if (tags.Length == 0)
        {
            EmitChanged(GetEditedProperty(), new Variant());
            return;
        }

        TagContainer current = ReadContainer();
        if (current != null && IsShared(current))
        {
            current.InitialTags = tags;
            Error saved = ResourceSaver.Save(current);
            if (saved != Error.Ok)
                GD.PushError($"Tags: could not save '{current.ResourcePath}' ({saved}).");

            UpdateProperty();
            return;
        }

        // A new embedded container each time, so undo puts the previous one back.
        EmitChanged(GetEditedProperty(), new TagContainer { InitialTags = tags });
    }

    private static string[] Without(string[] tags, string name)
    {
        var kept = new List<string>(tags.Length);
        foreach (string tag in tags)
        {
            if (tag != name)
                kept.Add(tag);
        }

        return kept.ToArray();
    }

    // Drops empty and repeated entries (left by the old array editor).
    private static string[] Clean(string[] tags)
    {
        var kept = new List<string>(tags.Length);
        foreach (string tag in tags)
        {
            if (!string.IsNullOrEmpty(tag) && !kept.Contains(tag))
                kept.Add(tag);
        }

        return kept.ToArray();
    }
}
#endif
