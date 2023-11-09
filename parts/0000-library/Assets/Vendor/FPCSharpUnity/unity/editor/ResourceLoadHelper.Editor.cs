#if UNITY_EDITOR
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.functional;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.editor; 

public partial class ResourceLoadHelper : AssetPostprocessor {
  static readonly Promise<Unit> _editor_domainLoadedPromise;
  static readonly Future<Unit> _editor_domainLoadedFuture;

  static ResourceLoadHelper() {
    _editor_domainLoadedFuture = Future.a(out _editor_domainLoadedPromise);
  }
  
  [UsedImplicitly]
  static void OnPostprocessAllAssets(
    string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths,
    bool didDomainReload
  ) {
    // https://docs.unity3d.com/2022.1/Documentation/Manual/UpgradeGuide2021LTS.html
    if (didDomainReload) {
      Debug.Log("Domain reloaded");
      _editor_domainLoadedPromise.tryComplete(Unit._);
    }
  }
}
#endif