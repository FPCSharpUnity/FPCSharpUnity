// ReSharper disable once CheckNamespace
namespace UnityEngine; 

// ReSharper disable once InconsistentNaming
public static class HideFlags_ {
  /// <summary>
  /// Same as <see cref="HideFlags.HideAndDontSave"/> but excludes flag <see cref="HideFlags.DontUnloadUnusedAsset"/>.
  /// </summary>
  public static readonly HideFlags hideAndDontSaveOnly = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset;
  
  /// <summary>
  /// Same as <see cref="HideFlags.DontSave"/> but excludes flag <see cref="HideFlags.DontUnloadUnusedAsset"/>.
  /// </summary>
  public static readonly HideFlags dontSaveOnly = HideFlags.DontSave & ~HideFlags.DontUnloadUnusedAsset;
}