using Godot;

namespace Tags;

/// <summary>
/// Autoload that loads <see cref="TagList"/> and initializes <see cref="TagRegistry"/> before the main scene.
/// </summary>
public partial class TagLoader : Node
{
    public override void _EnterTree()
    {
        if (TagRegistry.IsInitialized)
            return;

        string path = TagList.GetListPath();
        var tagList = GD.Load<TagList>(path);
        if (tagList == null)
        {
            GD.PushError($"TagLoader: could not load TagList at '{path}'.");
            return;
        }

        TagRegistry.Initialize(tagList.Tags);
    }
}
