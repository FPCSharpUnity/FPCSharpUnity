using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Utilities;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Editor.extensions {
  public struct EditorAssetInfo {
    public readonly PathStr path;
    public readonly string guid;

    public EditorAssetInfo(PathStr path, string guid) {
      this.path = path;
      this.guid = guid;
    }

    public override string ToString() =>
      $"{nameof(EditorAssetInfo)}[" +
      $"{nameof(path)}: {path}, " +
      $"{nameof(guid)}: {guid}" +
      $"]";
  }

  public struct EditorObjectInfo<A> where A : Object {
    public readonly A obj;
    /* Present if object is an asset on disk. */
    public readonly Option<EditorAssetInfo> assetInfo;

    public string name => obj.name;

    public EditorObjectInfo(A obj, Option<EditorAssetInfo> assetInfo) {
      this.obj = obj;
      this.assetInfo = assetInfo;
    }

    public override string ToString() =>
      $"{nameof(EditorObjectInfo<A>)}[" +
      $"{nameof(obj)}: {obj}, " +
      $"{nameof(assetInfo)}: {assetInfo}" +
      $"]";
  }

  public static class UnityObjectExts {
    public static EditorObjectInfo<A> debugInfo<A>(this A o) where A : Object {
      var pathOpt = AssetDatabase.GetAssetPath(o).opt().mapM(PathStr.a);
      var assetInfoOpt = pathOpt.mapM(path => {
        var guid = AssetDatabase.AssetPathToGUID(path);
        return new EditorAssetInfo(path, guid);
      });
      return new EditorObjectInfo<A>(o, assetInfoOpt);
    }

    [UsedImplicitly, MenuItem("Assets/FP C# Unity/Debug/Debug info")]
    public static void editorUtility() {
      var obj = F.opt(Selection.activeObject);
      obj.voidFoldM(
        () => EditorUtils.userInfo("No object selected!", "Please select an object!"),
        o => EditorUtils.userInfo($"Debug info for {o}", o.debugInfo().ToString())
      );
    }
  }
}