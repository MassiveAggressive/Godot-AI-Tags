#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Tags;

/// <summary>
/// Editor window listing the tags of Tags.tres as a tree (parents from the names), opened right under the edited
/// field. On top, a box adds a new tag to Tags.tres. In multi mode every tag has a check box and each change reports
/// the whole selection (<see cref="SelectionChanged"/>); in single mode a click (or Enter) picks the tag and closes
/// the window (<see cref="TagPicked"/>). A parent is a tag of its own: checking it does not check its children. A
/// parent whose children are checked shows as partly checked.
/// </summary>
[Tool]
public partial class TagTreePopup : PopupPanel
{
    private const float BaseWidth = 360f;
    private const float BaseHeight = 460f;

    private readonly LineEdit _newTag;
    private readonly Button _add;
    private readonly Label _newError;
    private readonly LineEdit _search;
    private readonly Button _toggleAll;
    private Texture2D _expandIcon;
    private Texture2D _collapseIcon;
    private bool _allExpanded;
    private readonly Tree _tree;
    private readonly Label _empty;
    private readonly HBoxContainer _footer;
    private readonly Label _summary;
    private readonly Button _clearAll;

    // Selection in the order the tags were picked, so the field keeps its order.
    private readonly List<string> _order = new();
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);

    // Every tag's item by full name, rebuilt on each opening (Tags.tres may have changed).
    private readonly Dictionary<string, TreeItem> _items = new(StringComparer.Ordinal);
    private bool _multi;
    private bool _updating;
    private bool _styled;

    public TagTreePopup()
    {
        var margin = new MarginContainer();
        AddChild(margin);

        var box = new VBoxContainer();
        margin.AddChild(box);

        var addRow = new HBoxContainer();
        box.AddChild(addRow);
        _newTag = new LineEdit { PlaceholderText = "New tag, e.g. State.Debuff.Stun", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _newTag.TextSubmitted += _ => AddNewTag();
        _newTag.TextChanged += _ => _newError.Visible = false;
        addRow.AddChild(_newTag);
        _add = new Button { Text = "Add", TooltipText = "Add this tag to the tag list" };
        _add.Pressed += AddNewTag;
        addRow.AddChild(_add);

        _newError = new Label { Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(_newError);

        box.AddChild(new HSeparator());

        var searchRow = new HBoxContainer();
        box.AddChild(searchRow);
        _search = new LineEdit { PlaceholderText = "Search", ClearButtonEnabled = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _search.TextChanged += ApplyFilter;
        searchRow.AddChild(_search);
        _toggleAll = new Button { Flat = true };
        _toggleAll.Pressed += () => SetAllExpanded(!_allExpanded);
        searchRow.AddChild(_toggleAll);

        _tree = new Tree { HideRoot = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _tree.ItemEdited += OnItemEdited;
        _tree.ItemMouseSelected += (_, button) => OnPick(button == (long)MouseButton.Left);
        _tree.ItemActivated += () => OnPick(true);
        box.AddChild(_tree);

        _empty = new Label
        {
            Text = "No tags yet. Type one above (e.g. State.Stunned) and press Add.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
        };
        box.AddChild(_empty);

        _footer = new HBoxContainer();
        box.AddChild(_footer);
        _summary = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _footer.AddChild(_summary);
        _clearAll = new Button { Text = "Clear", TooltipText = "Remove every selected tag" };
        _clearAll.Pressed += ClearAll;
        _footer.AddChild(_clearAll);
    }

    /// <summary>Multi mode: every selected tag name, in order, after each change.</summary>
    public event Action<string[]> SelectionChanged;

    /// <summary>Single mode: the picked tag name; the window closes.</summary>
    public event Action<string> TagPicked;

    /// <summary>
    /// Opens under <paramref name="anchor"/> (the field's screen rect), at least as wide as it, with
    /// <paramref name="selected"/> checked (multi) or highlighted (single).
    /// </summary>
    public void Open(IEnumerable<string> selected, bool multi, Rect2I anchor)
    {
        ApplyStyle();
        _multi = multi;
        _order.Clear();
        _selected.Clear();
        foreach (string name in selected)
        {
            if (!string.IsNullOrEmpty(name) && _selected.Add(name))
                _order.Add(name);
        }

        _search.Text = string.Empty;
        _newTag.Text = string.Empty;
        _newError.Visible = false;
        _footer.Visible = multi;
        Build();

        Popup(PlaceUnder(anchor));
        _search.GrabFocus();
    }

    private Rect2I PlaceUnder(Rect2I anchor)
    {
        float scale = EditorInterface.Singleton.GetEditorScale();
        var size = new Vector2I(Math.Max(anchor.Size.X, (int)(BaseWidth * scale)), (int)(BaseHeight * scale));
        var position = new Vector2I(anchor.Position.X, anchor.End.Y);

        Rect2I screen = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        if (position.Y + size.Y > screen.End.Y)
            position.Y = Math.Max(screen.Position.Y, anchor.Position.Y - size.Y);

        position.X = Math.Clamp(position.X, screen.Position.X, Math.Max(screen.Position.X, screen.End.X - size.X));
        return new Rect2I(position, size);
    }

    /// <summary>Editor theme looks, set once when first opened (the theme is ready by then).</summary>
    private void ApplyStyle()
    {
        if (_styled)
            return;

        _styled = true;
        Theme theme = EditorInterface.Singleton.GetEditorTheme();
        float scale = EditorInterface.Singleton.GetEditorScale();
        int pad = (int)(8 * scale);
        var margin = GetChild<MarginContainer>(0);
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, pad);

        _search.RightIcon = Icon(theme, "Search");
        _add.Icon = Icon(theme, "Add");
        _expandIcon = Icon(theme, "ExpandTree");
        _collapseIcon = Icon(theme, "CollapseTree");

        _tree.AddThemeConstantOverride("v_separation", (int)(5 * scale));
        _tree.AddThemeConstantOverride("item_margin", (int)(20 * scale));
        _tree.AddThemeConstantOverride("draw_relationship_lines", 1);
        _tree.AddThemeConstantOverride("relationship_line_width", Math.Max(1, (int)scale));
        _tree.AddThemeColorOverride("relationship_line_color", theme.GetColor("font_disabled_color", "Editor"));

        _newError.AddThemeColorOverride("font_color", theme.GetColor("error_color", "Editor"));
        _empty.AddThemeColorOverride("font_color", theme.GetColor("font_disabled_color", "Editor"));
        _summary.AddThemeColorOverride("font_color", theme.GetColor("font_disabled_color", "Editor"));
    }

    private static Texture2D Icon(Theme theme, string name) => theme.HasIcon(name, "EditorIcons") ? theme.GetIcon(name, "EditorIcons") : null;

    /// <summary>The toggle shows what pressing it does next: expand while collapsed, collapse while expanded.</summary>
    private void RefreshToggle()
    {
        Texture2D icon = _allExpanded ? _collapseIcon : _expandIcon;
        _toggleAll.Icon = icon;
        _toggleAll.Text = icon != null ? string.Empty : _allExpanded ? "−" : "+";
        _toggleAll.TooltipText = _allExpanded ? "Collapse all" : "Expand all";
    }

    private void Build()
    {
        _updating = true;
        _tree.Clear();
        _items.Clear();
        TreeItem root = _tree.CreateItem();
        Theme theme = EditorInterface.Singleton.GetEditorTheme();
        Font bold = theme.GetFont("bold", "EditorFonts");

        // A light band behind every row (from the font color, so it shows on dark and light themes); the row
        // spacing between the bands keeps the rows apart.
        Color row = new(theme.GetColor("font_color", "Editor"), 0.08f);

        // Sorted, so a parent always comes before its children.
        foreach (string name in TagContainer.CollectTagNames())
        {
            int dot = name.LastIndexOf('.');
            TreeItem parent = dot > 0 && _items.TryGetValue(name.Substring(0, dot), out TreeItem found) ? found : root;
            TreeItem item = _tree.CreateItem(parent);

            // First: changing the cell mode clears the cell's text.
            if (_multi)
            {
                item.SetCellMode(0, TreeItem.TreeCellMode.Check);
                item.SetEditable(0, true);
                item.SetChecked(0, _selected.Contains(name));
            }

            item.SetText(0, dot > 0 ? name.Substring(dot + 1) : name);
            item.SetTooltipText(0, name);
            item.SetMetadata(0, name);
            item.SetCustomBgColor(0, row);
            if (parent == root && bold != null)
                item.SetCustomFont(0, bold);

            _items[name] = item;
        }

        // Open the way to every selected tag, keep the rest closed.
        foreach (TreeItem item in _items.Values)
            item.Collapsed = true;

        _allExpanded = false;
        RefreshToggle();

        foreach (string name in _selected)
        {
            if (_items.TryGetValue(name, out TreeItem item))
                Reveal(item);
        }

        bool none = _items.Count == 0;
        _tree.Visible = !none;
        _empty.Visible = none;
        RefreshMarks();
        _updating = false;
    }

    private static void Reveal(TreeItem item)
    {
        for (TreeItem parent = item.GetParent(); parent != null; parent = parent.GetParent())
            parent.Collapsed = false;
    }

    private void OnItemEdited()
    {
        if (_updating || !_multi)
            return;

        TreeItem item = _tree.GetEdited();
        SetSelected(item.GetMetadata(0).AsString(), item.IsChecked(0));
    }

    private void OnPick(bool picked)
    {
        TreeItem item = _tree.GetSelected();
        if (_updating || _multi || !picked || item == null)
            return;

        string name = item.GetMetadata(0).AsString();
        Hide();
        TagPicked?.Invoke(name);
    }

    private void SetSelected(string name, bool on)
    {
        if (on && _selected.Add(name))
            _order.Add(name);
        else if (!on && _selected.Remove(name))
            _order.Remove(name);
        else
            return;

        RefreshMarks();
        SelectionChanged?.Invoke(_order.ToArray());
    }

    private void ClearAll()
    {
        if (_selected.Count == 0)
            return;

        _selected.Clear();
        _order.Clear();
        _updating = true;
        foreach (TreeItem item in _items.Values)
            item.SetChecked(0, false);

        _updating = false;
        RefreshMarks();
        SelectionChanged?.Invoke(Array.Empty<string>());
    }

    /// <summary>Selected tags in the accent color, their unselected parents partly checked (multi) or dimly accented (single).</summary>
    private void RefreshMarks()
    {
        Theme theme = EditorInterface.Singleton.GetEditorTheme();
        Color accent = theme.GetColor("accent_color", "Editor");
        Color partialColor = new(accent, 0.65f);
        foreach (KeyValuePair<string, TreeItem> pair in _items)
        {
            bool selected = _selected.Contains(pair.Key);
            bool partial = !selected && HasSelectedChild(pair.Key);
            if (_multi)
                pair.Value.SetIndeterminate(0, partial);

            if (selected)
                pair.Value.SetCustomColor(0, accent);
            else if (partial)
                pair.Value.SetCustomColor(0, partialColor);
            else
                pair.Value.ClearCustomColor(0);
        }

        _summary.Text = _selected.Count == 1 ? "1 selected" : $"{_selected.Count} selected";
        _clearAll.Disabled = _selected.Count == 0;
    }

    private bool HasSelectedChild(string parent)
    {
        foreach (string name in _selected)
        {
            if (name.Length > parent.Length && name[parent.Length] == '.' && name.StartsWith(parent, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>Shows the tags whose full name contains the text, with their parents, all opened; empty shows all.</summary>
    private void ApplyFilter(string text)
    {
        foreach (KeyValuePair<string, TreeItem> pair in _items)
            pair.Value.Visible = text.Length == 0;

        if (text.Length == 0)
            return;

        foreach (KeyValuePair<string, TreeItem> pair in _items)
        {
            if (!pair.Key.Contains(text, StringComparison.OrdinalIgnoreCase))
                continue;

            for (TreeItem item = pair.Value; item != null && item != _tree.GetRoot(); item = item.GetParent())
            {
                item.Visible = true;
                item.Collapsed = false;
            }
        }
    }

    private void SetAllExpanded(bool expanded)
    {
        _allExpanded = expanded;
        foreach (TreeItem item in _items.Values)
            item.Collapsed = !expanded;

        RefreshToggle();
    }

    // -------------------------------------------------------------- new tag

    private void AddNewTag()
    {
        string name = _newTag.Text.Trim();
        string error = CheckNewName(name);
        if (error == null)
            error = SaveToList(name);

        if (error != null)
        {
            _newError.Text = error;
            _newError.Visible = true;
            return;
        }

        _newTag.Text = string.Empty;
        _newError.Visible = false;
        _search.Text = string.Empty;
        Build();

        if (_items.TryGetValue(name, out TreeItem item))
        {
            Reveal(item);
            _tree.ScrollToItem(item);
        }

        if (_multi)
        {
            if (item != null)
            {
                _updating = true;
                item.SetChecked(0, true);
                _updating = false;
            }

            SetSelected(name, true);
            return;
        }

        Hide();
        TagPicked?.Invoke(name);
    }

    private static string CheckNewName(string name)
    {
        if (name.Length == 0)
            return "Type a tag name first.";

        if (!TagRegistry.IsValidName(name))
            return "Use words joined by dots, without spaces, ',' or ':' (e.g. State.Stunned).";

        if (TagContainer.CollectTagNames().Contains(name))
            return $"'{name}' already exists.";

        return null;
    }

    /// <summary>Appends the tag to the tag list file (creating it if missing) and saves it; an error message or null.</summary>
    private static string SaveToList(string name)
    {
        string path = TagList.GetListPath();
        TagList list = ResourceLoader.Exists(path) ? ResourceLoader.Load(path) as TagList : null;
        if (list == null && ResourceLoader.Exists(path))
            return $"'{path}' is not a TagList; check the tags/tag_list_path setting.";

        bool created = list == null;
        if (created)
        {
            list = new TagList();
            DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
        }

        string[] tags = list.Tags ?? Array.Empty<string>();
        var updated = new string[tags.Length + 1];
        Array.Copy(tags, updated, tags.Length);
        updated[tags.Length] = name;
        list.Tags = updated;

        Error saved = ResourceSaver.Save(list, path, created ? ResourceSaver.SaverFlags.ChangePath : ResourceSaver.SaverFlags.None);
        if (saved != Error.Ok)
        {
            list.Tags = tags;
            return $"Could not save '{path}' ({saved}).";
        }

        EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(path);
        return null;
    }
}
#endif
