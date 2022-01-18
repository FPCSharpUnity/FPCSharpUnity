using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class RendererExts {
    public delegate int RendererGetMaterialCount(Renderer self);
    public static readonly RendererGetMaterialCount GetMaterialCount = 
      (RendererGetMaterialCount) System.Delegate.CreateDelegate(
        type: typeof(RendererGetMaterialCount), firstArgument: null, 
        // ReSharper disable once AssignNullToNotNullAttribute
        method: typeof(Renderer).GetMethod("GetMaterialCount", System.Reflection.BindingFlags.NonPublic)
      );

    public static int GetMaterialCountReflected(this Renderer r) => GetMaterialCount(r);
  }
}