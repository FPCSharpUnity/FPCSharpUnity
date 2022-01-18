﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities {
  public static class SceneManagerUtils {
    public static IEnumerable<Scene> loadedScenes =>
      Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt);
  }
}
