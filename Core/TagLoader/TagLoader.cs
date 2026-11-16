using Godot;

namespace AbilitySystem;

/// <summary>
/// Autoload that loads <see cref="TagList"/> and initializes <see cref="TagRegistry"/> before the main scene.
/// </summary>
public partial class TagLoader : Node
{
    public override void _EnterTree()
    {
        if (TagRegistry.IsInitialized)
            return;

        var tagList = GD.Load<TagList>(TagList.DefaultPath);
        if (tagList == null)
        {
            GD.PushError($"TagLoader: could not load TagList at '{TagList.DefaultPath}'.");
            return;
        }

        TagRegistry.Initialize(tagList.Tags);
    }
}
