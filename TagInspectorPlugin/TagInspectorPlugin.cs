#if TOOLS
using Godot;

namespace Tags;

/// <summary>
/// Puts the tag tree editors into the inspector: <see cref="TagContainerProperty"/> for every
/// <see cref="TagContainer"/> field (empty or not) and for <see cref="TagContainer.InitialTags"/>,
/// <see cref="TagNameProperty"/> for string fields whose hint string is
/// <see cref="TagContainer.TagNameHint"/>. Registered by <see cref="TagsPlugin"/>.
/// </summary>
[Tool]
public partial class TagInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object) => true;

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType,
        string hintString, PropertyUsageFlags usageFlags, bool wide)
    {
        if (@object is TagContainer && name == nameof(TagContainer.InitialTags))
        {
            AddPropertyEditor(name, new TagContainerProperty());
            return true;
        }

        // A TagContainer field of any resource or node, also while it is still empty.
        if (type == Variant.Type.Object && hintType == PropertyHint.ResourceType && hintString == nameof(TagContainer))
        {
            AddPropertyEditor(name, new TagContainerProperty { EditsResource = true });
            return true;
        }

        if (type == Variant.Type.String && hintString == TagContainer.TagNameHint)
        {
            AddPropertyEditor(name, new TagNameProperty());
            return true;
        }

        return false;
    }
}
#endif
