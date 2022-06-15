using System.Collections.Generic;
using GenerationAttributes;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities; 

/// <summary>
/// Allows you to iterate through all of the currently loaded scenes using `foreach`.
/// </summary>
[Record(ConstructorFlags.Constructor)]
public readonly partial struct SceneManagerLoadedScenes {
  public readonly LoadedSceneCount loadedSceneCount;
  
  public SceneManagerLoadedScenesEnumerator GetEnumerator() => new(loadedSceneCount);

  /// <summary>Converts this into <see cref="IEnumerable{T}"/>.</summary>
  /// <note><b>This allocates on the heap.</b></note>
  public IEnumerable<Scene> asEnumerable {
    get {
      foreach (var scene in this) yield return scene;
    }
  }
}

public struct SceneManagerLoadedScenesEnumerator {
  readonly LoadedSceneCount loadedSceneCount;
  int currentSceneIdx;

  public SceneManagerLoadedScenesEnumerator(LoadedSceneCount loadedSceneCount) {
    this.loadedSceneCount = loadedSceneCount;
    currentSceneIdx = -1;
  }

  public bool MoveNext() {
    currentSceneIdx++;
    return currentSceneIdx < loadedSceneCount;
  }

  public void Reset() => currentSceneIdx = -1;
  
  public Scene Current =>  SceneManagerBetter.instance.getLoadedSceneAtE(currentSceneIdx).rightOrThrow;
}