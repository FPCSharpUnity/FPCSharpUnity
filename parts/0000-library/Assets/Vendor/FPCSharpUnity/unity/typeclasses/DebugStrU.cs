using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.typeclasses;
using GenerationAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.typeclasses; 

/// <summary>
/// Instances of <see cref="DebugStr{A}"/> for Unity types.
/// </summary>
public static class DebugStrU {
  [Implicit, LazyProperty] public static DebugStr<Scene> scene =>
    DebugStr.lambda((Scene s) =>
      $"Scene("
      + $"{s.name.echoNameOf()}, {s.path.echoNameOf()}, {s.buildIndex.echoNameOf()}, valid={s.IsValid()}, "
      + $"loaded={s.isLoaded}"
      + $")"
    );
  
  [Implicit, LazyProperty] public static DebugStr<AsyncOperation> asyncOperation => 
    DebugStr.lambda((AsyncOperation op) =>
      $"AsyncOperation("
      + $"{op.progress.echoNameOf()}, {op.isDone.echoNameOf()}, {op.priority.echoNameOf()}, "
      + $"{op.allowSceneActivation.echoNameOf()}"
      + $")"
    );
}