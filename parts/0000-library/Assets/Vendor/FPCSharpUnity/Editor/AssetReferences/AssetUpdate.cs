using System;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.unity.Data;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Editor.AssetReferences {
  public partial class AssetUpdate {
    [Record(ConstructorFlags.Constructor)] public readonly partial struct Move {
      public readonly AssetPath fromPath, toPath;
    }

    public readonly ImmutableArray<AssetPath> newAssets, deletedAssets;
    public readonly ImmutableArray<Move> movedAssets;
    public readonly int totalAssets;

    public AssetUpdate(
      ImmutableArray<AssetPath> newAssets, ImmutableArray<AssetPath> deletedAssets,
      ImmutableArray<Move> movedAssets
    ) {
      this.newAssets = newAssets;
      this.deletedAssets = deletedAssets;
      this.movedAssets = movedAssets;
      totalAssets = newAssets.Length + deletedAssets.Length + movedAssets.Length;
    }

    public static AssetUpdate fromAllAssets(ImmutableArray<AssetPath> allAssets) => new AssetUpdate(
      allAssets, ImmutableArray<AssetPath>.Empty, ImmutableArray<Move>.Empty
    );

    public AssetUpdate filter(
      Func<AssetPath, bool> predicate,
      Func<Move, bool> movePredicate
    ) => new AssetUpdate(
      newAssets.Where(predicate).ToImmutableArray(),
      deletedAssets.Where(predicate).ToImmutableArray(),
      movedAssets.Where(movePredicate).ToImmutableArray()
    );
  }
}