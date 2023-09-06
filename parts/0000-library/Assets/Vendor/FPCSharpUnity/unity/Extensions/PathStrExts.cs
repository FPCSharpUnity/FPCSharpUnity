using System.IO;
using FPCSharpUnity.core.data;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions; 

public static class PathStrExts {
  /// <summary>
  /// Use this with Unity Resources, AssetDatabase and PrefabUtility methods
  /// </summary>
  public static string unityPath(this PathStr p) => 
    Path.DirectorySeparatorChar == '/' ? p.path : p.path.Replace('\\' , '/');
  
#if UNITY_EDITOR
  /// <summary>Relative directory to the `Unity Assets` folder (E.g. `relative_path/unity/Assets/`).</summary>
  public static readonly PathStr editor__unityAssetsDirectory = PathStr.a(Application.dataPath);
    
  /// <summary>Relative directory to the `Unity Project` folder (E.g. `relative_path/unity/`).</summary>
  public static readonly PathStr editor__unityProjectDirectory = PathStr.a(Application.dataPath) / "..";
#endif
}