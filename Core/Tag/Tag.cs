using System;

namespace AbilitySystem;

/// <summary>
/// Hierarchical tag such as "Ability.Fire.Ball". Holds only a numeric id; create through <see cref="TagRegistry.Request"/>.
/// </summary>
public readonly struct Tag : IEquatable<Tag>
{
    public static readonly Tag None = default;

    public int Id { get; }

    public bool IsValid => Id != 0;

    internal Tag(int id)
    {
        Id = id;
    }

    /// <summary>True if this tag is <paramref name="parent"/> or one of its children.</summary>
    public bool Matches(Tag parent) => TagRegistry.Matches(this, parent);

    public bool MatchesExact(Tag other) => IsValid && Id == other.Id;

    public bool Equals(Tag other) => Id == other.Id;

    public override bool Equals(object obj) => obj is Tag other && Id == other.Id;

    public override int GetHashCode() => Id;

    public override string ToString() => TagRegistry.GetName(this);

    public static bool operator ==(Tag left, Tag right) => left.Id == right.Id;

    public static bool operator !=(Tag left, Tag right) => left.Id != right.Id;
}
