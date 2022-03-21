using System;
using System.Collections.Immutable;
using System.Linq;

namespace FPCSharpUnity.unity.Editor.AssetReferences {
  public class AssetUpdate {
    public struct Move {
      public readonly string fromPath, toPath;

      public Move(string fromPath, string toPath) {
        this.fromPath = fromPath;
        this.toPath = toPath;
      }
    }

    public readonly ImmutableArray<string> newAssets, deletedAssets;
    public readonly ImmutableArray<Move> movedAssets;
    public readonly int totalAssets;

    public AssetUpdate(
      ImmutableArray<string> newAssets, ImmutableArray<string> deletedAssets,
      ImmutableArray<Move> movedAssets
    ) {
      this.newAssets = newAssets;
      this.deletedAssets = deletedAssets;
      this.movedAssets = movedAssets;
      totalAssets = newAssets.Length + deletedAssets.Length + movedAssets.Length;
    }

    public static AssetUpdate fromAllAssets(ImmutableArray<string> allAssets) => new AssetUpdate(
      allAssets, ImmutableArray<string>.Empty, ImmutableArray<Move>.Empty
    );

    public AssetUpdate filter(
      Func<string, bool> predicate,
      Func<Move, bool> movePredicate
    ) => new AssetUpdate(
      newAssets.Where(predicate).ToImmutableArray(),
      deletedAssets.Where(predicate).ToImmutableArray(),
      movedAssets.Where(movePredicate).ToImmutableArray()
    );
  }
}