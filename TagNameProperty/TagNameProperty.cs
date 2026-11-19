#if TOOLS
using Godot;

namespace Tags;

/// <summary>
/// Inspector editor of a single tag name field marked with <see cref="TagContainer.TagNameHint"/>: a button showing
/// the tag that opens the tag tree in single mode right under the field (a new tag added there is picked), and a
/// clear button. A name no longer in Tags.tres is shown in the warning color.
/// </summary>
[Tool]
public partial class TagNameProperty : EditorProperty
{
    private readonly Button _pick;
    private readonly Button _clear;
    private readonly TagTreePopup _popup;

    public TagNameProperty()
    {
        var row = new HBoxContainer();
        _pick = new Button { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true, Alignment = HorizontalAlignment.Left };
        _pick.Pressed += () => _popup.Open(new[] { ReadName() }, multi: false, new Rect2I((Vector2I)GetScreenPosition(), (Vector2I)Size));
        row.AddChild(_pick);

        _clear = new Button { Text = "×", Flat = true, TooltipText = "Clear" };
        _clear.Pressed += () => EmitChanged(GetEditedProperty(), string.Empty);
        row.AddChild(_clear);

        AddChild(row);
        AddFocusable(_pick);

        _popup = new TagTreePopup();
        _popup.TagPicked += name => EmitChanged(GetEditedProperty(), name);
        AddChild(_popup);
    }

    public override void _UpdateProperty()
    {
        string name = ReadName();
        bool empty = string.IsNullOrEmpty(name);
        bool known = empty || TagContainer.CollectTagNames().Contains(name);

        _pick.Text = empty ? "(none)" : name;
        _pick.TooltipText = known ? name : $"'{name}' is not in the tag list.";
        if (known)
            _pick.RemoveThemeColorOverride("font_color");
        else
            _pick.AddThemeColorOverride("font_color", GetThemeColor("warning_color", "Editor"));

        bool readOnly = IsReadOnly();
        _pick.Disabled = readOnly;
        _clear.Disabled = readOnly || empty;
    }

    private string ReadName() => GetEditedObject().Get(GetEditedProperty()).AsString();
}
#endif
