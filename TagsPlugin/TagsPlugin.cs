#if TOOLS
using Godot;

namespace Tags;

/// <summary>
/// Editor plugin: registers the TagLoader autoload, the "tags/tag_list_path" project setting and the tag tree
/// editors of the inspector (<see cref="TagInspectorPlugin"/>).
/// </summary>
[Tool]
public partial class TagsPlugin : EditorPlugin
{
    private const string AutoloadName = "TagLoader";
    private const string AutoloadPath = "res://addons/Tags/TagLoader/TagLoader.cs";

    private TagInspectorPlugin _inspector;

    public override void _EnterTree()
    {
        RegisterPathSetting();
        _inspector = new TagInspectorPlugin();
        AddInspectorPlugin(_inspector);
    }

    public override void _ExitTree()
    {
        if (_inspector == null)
            return;

        RemoveInspectorPlugin(_inspector);
        _inspector = null;
    }

    public override void _EnablePlugin()
    {
        RegisterPathSetting();
        AddAutoloadSingleton(AutoloadName, AutoloadPath);
    }

    public override void _DisablePlugin()
    {
        RemoveAutoloadSingleton(AutoloadName);
    }

    private static void RegisterPathSetting()
    {
        if (!ProjectSettings.HasSetting(TagList.PathSetting))
            ProjectSettings.SetSetting(TagList.PathSetting, TagList.DefaultPath);

        ProjectSettings.SetInitialValue(TagList.PathSetting, TagList.DefaultPath);
        ProjectSettings.SetAsBasic(TagList.PathSetting, true);

        // Godot API: property info must be passed as a Godot Dictionary.
        ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
        {
            { "name", TagList.PathSetting },
            { "type", (int)Variant.Type.String },
            { "hint", (int)PropertyHint.File },
            { "hint_string", "*.tres,*.res" },
        });
    }
}
#endif
