using System;
using Godot;

namespace Tags;

/// <summary>
/// Editor-side list of every tag in the project. The file path comes from the "tags/tag_list_path" project setting.
/// </summary>
[Tool]
[GlobalClass]
public partial class TagList : Resource
{
    public const string PathSetting = "tags/tag_list_path";
    public const string DefaultPath = "res://Resource/Tags/Tags.tres";

    // Godot Resource and [Export] are required here so the list can be edited and saved in the editor.
    [Export]
    public string[] Tags { get; set; } = Array.Empty<string>();

    public static string GetListPath() => ProjectSettings.GetSetting(PathSetting, DefaultPath).AsString();
}
