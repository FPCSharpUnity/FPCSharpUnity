using System.Collections.Generic;
using FPCSharpUnity.core.collection;
using GenerationAttributes;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities; 

/// <summary>
/// Allows you to iterate through all of the currently loaded scenes using `foreach`.
/// </summary>
[Record(ConstructorFlags.Constructor)]
public readonly partial struct SceneManagerLoadedScenes {
  public readonly LoadedSceneCount loadedSceneCount;
  
  public CollectionEnumerator<Scene> GetEnumerator() => 
    new(static idx => SceneManagerBetter.instance.getLoadedSceneAtE(idx).rightOrThrow, loadedSceneCount);

  /// <summary>Converts this into <see cref="IEnumerable{T}"/>.</summary>
  /// <note><b>This allocates on the heap.</b></note>
  public IEnumerable<Scene> asEnumerable {
    get {
      foreach (var scene in this) yield return scene;
    }
  }
}