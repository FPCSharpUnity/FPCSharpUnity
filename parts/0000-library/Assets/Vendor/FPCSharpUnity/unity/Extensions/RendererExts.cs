using System;
using GenerationAttributes;
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
    
    [LazyProperty] static MaterialPropertyBlock cachedMpb => new();
  
    public static void updatePropertyBlock<A>(this Renderer renderer, A data, Action<MaterialPropertyBlock, A> modifier) {
      renderer.GetPropertyBlock(cachedMpb);
      modifier(cachedMpb, data);
      renderer.SetPropertyBlock(cachedMpb);
    }
  }
}