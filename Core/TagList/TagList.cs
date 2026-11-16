using System;
using Godot;

namespace AbilitySystem;

/// <summary>
/// Editor-side list of every tag in the project. Data lives in res://Resource/Tags/Tags.tres.
/// </summary>
[Tool]
[GlobalClass]
public partial class TagList : Resource
{
    public const string DefaultPath = "res://Resource/Tags/Tags.tres";

    // Godot Resource and [Export] are required here so the list can be edited and saved in the editor.
    [Export]
    public string[] Tags { get; set; } = Array.Empty<string>();
}
