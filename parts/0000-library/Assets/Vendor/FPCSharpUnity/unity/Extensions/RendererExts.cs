using System;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.core.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using static System.Reflection.BindingFlags;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static class RendererExts {
    public delegate int RendererGetMaterialCount(Renderer self);
    public static readonly RendererGetMaterialCount GetMaterialCount = 
      (RendererGetMaterialCount) Delegate.CreateDelegate(
        type: typeof(RendererGetMaterialCount), firstArgument: null, 
        // ReSharper disable once AssignNullToNotNullAttribute
        method: typeof(Renderer).GetMethod("GetMaterialCount", NonPublic | Instance)
      );

    public static int GetMaterialCountReflected(this Renderer r) => GetMaterialCount(r);
    
    [LazyProperty] static MaterialPropertyBlock cachedMpb => new();

    /// <param name="shaderPropertyName">
    /// Name of the property in the shader. Probably acquired from <see cref="ShaderPropertyAttribute"/>.
    /// </param>
    public delegate void SetPropertyValue<in A>(
      MaterialPropertyBlock materialPropertyBlock, string shaderPropertyName, A value
    );
    
    /// <summary>
    /// You will probably invoke this on Unity <see cref="IMB_Update"/> callback, so make sure to not allocate a
    /// closure for <see cref="set"/> function!
    /// </summary>
    /// <example><code><![CDATA[
    /// _target.updatePropertyBlock(_shaderProperty, value, static (mpb, prop, v) => mpb.SetFloat(prop, v));
    /// ]]></code></example>
    public static void updatePropertyBlock<A>(
      this Renderer renderer, string shaderPropertyName, A data, SetPropertyValue<A> set
    ) {
      renderer.GetPropertyBlock(cachedMpb);
      set(cachedMpb, shaderPropertyName, data);
      renderer.SetPropertyBlock(cachedMpb);
    }
    
    /// <summary>
    /// Function which tells how to extract the value we need from the <see cref="MaterialPropertyBlock"/> object.
    /// </summary>
    /// <param name="shaderPropertyName">
    /// Name of the property in the shader. Probably acquired from <see cref="ShaderPropertyAttribute"/>.
    /// </param>
    public delegate A GetPropertyValue<out A>(MaterialPropertyBlock materialPropertyBlock, string shaderPropertyName);
    
    /// <summary>
    /// You will probably invoke this on Unity <see cref="IMB_Update"/> callback, so make sure to not allocate a
    /// closure for <see cref="get"/> function!
    /// </summary>
    /// <example><code><![CDATA[
    /// _target.getPropertyValue(_shaderProperty, static (mpb, prop) => mpb.GetFloat(prop));
    /// ]]></code></example>
    public static A getPropertyValue<A>(
      this Renderer renderer, string shaderPropertyName, GetPropertyValue<A> get
    ) {
      renderer.GetPropertyBlock(cachedMpb);
      return get(cachedMpb, shaderPropertyName);
    }
  }
}