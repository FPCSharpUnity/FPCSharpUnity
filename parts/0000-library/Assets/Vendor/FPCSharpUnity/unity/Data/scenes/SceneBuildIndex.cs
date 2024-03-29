using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Data.scenes {
  /// <summary>Index of a scene in the build settings. Obtained via <see cref="SceneExts.sceneBuildIndex"/>.</summary>
  [Record(ConstructorFlags.Constructor), NewTypeImplicitTo]
  public readonly partial struct SceneBuildIndex : IStr { 
    public readonly int index;

    public string asString() => index.ToString();
  }

  /// <summary>Failure case for <see cref="SceneExts.sceneBuildIndex"/>.</summary>
  public enum SceneBuildIndexError {
    /// <summary>The scene is not in the build scenes list and is instead loaded through an asset bundle.</summary>
    SceneLoadedThroughAssetBundle,
    /// <summary>The scene is not in the build scenes list.</summary>
    SceneNotIncludedInBuildScenesList
  }
}