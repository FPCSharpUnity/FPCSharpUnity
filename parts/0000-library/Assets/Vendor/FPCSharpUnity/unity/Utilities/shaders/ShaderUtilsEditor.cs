#if UNITY_EDITOR
using System;
using System.Linq;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.core.Utilities {
  /// <summary>
  /// <b>!!! BEWARE !!!</b> This code uses Unity Editor code and if used from non-editor assembly definition
  /// it should be used within the `#if UNITY_EDITOR` preprocessor directive!
  /// </summary>
  [PublicAPI] public static partial class ShaderUtilsEditor {
    /// <summary>Computed shader property from shader.</summary>
    // This value might be computed a lot on Editor update, So we use struct instead of class
    [Record] public readonly partial struct ShaderProperty {
      public readonly string name;
      public readonly ShaderPropertyAttribute.Type type;
    }
    
    [Record] public readonly partial struct ShaderPropertyValidationError {
      public readonly string message;
    }
    
    /// <summary>Defines from where can we compute all shader properties.</summary>
    [Matcher] public interface IComputeFromShaderProperties { }

    [Record] public sealed partial class ComputeFromRenderer : IComputeFromShaderProperties {
      public readonly Renderer a;
      // Helper method for upcasting, to avoid boxing allocation.
      public IComputeFromShaderProperties up => this;
      
      public static implicit operator Renderer(ComputeFromRenderer computeFrom) => computeFrom.a;
      public static implicit operator ComputeFromRenderer(Renderer value) => new(value);
    }

    [Record] public sealed partial class ComputeFromShader : IComputeFromShaderProperties {
      public readonly Shader a;
      // Helper method for upcasting, to avoid boxing allocation.
      public IComputeFromShaderProperties up => this;
      
      public static implicit operator Shader(ComputeFromShader computeFrom) => computeFrom.a;
      public static implicit operator ComputeFromShader(Shader value) => new(value);
    }

    /// <summary>Computing all properties from <see cref="IComputeFromShaderProperties"/>.</summary>
    public static ShaderProperty[] computeAllShaderProperties(IComputeFromShaderProperties computeFrom) =>
      computeFrom.match(
        computeFromRenderer: computeFromRenderer => computeAllShaderProperties(computeFromRenderer.a),
        computeFromShader: computeFromShader => computeAllShaderPropertiesFromSingleShader(computeFromShader.a)
      );
    
    /// <summary>
    /// Computes all shader property names of <see cref="ShaderUtil.ShaderPropertyType"/> type,
    /// from <see cref="IComputeFromShaderProperties"/>.
    /// </summary>
    public static string[] computeAllShaderPropertyNamesForType(
      IComputeFromShaderProperties computeFrom, ShaderPropertyAttribute.Type shaderPropertyType
    ) => computeAllShaderProperties(computeFrom)
      .Where(_ => _.type == shaderPropertyType)
      .Select(_ => _.name)
      .ToArray();

    static ShaderProperty[] computeAllShaderProperties(Renderer renderer) =>
      renderer != null 
        // This is taken in consideration, that shader property will be set using 'MaterialBlockProperty'
        // Which handles setting correct value, on correct material. 
        ? computeAllShaderProperties(renderer.sharedMaterials.Select(_ => _.shader).ToArray())
        : Array.Empty<ShaderProperty>();

    static ShaderProperty[] computeAllShaderProperties(Shader[] shaders) =>
      shaders.SelectMany(computeAllShaderPropertiesFromSingleShader).ToArray();
    
    static ShaderProperty[] computeAllShaderPropertiesFromSingleShader(Shader shader) =>
      shader != null
        ? Enumerable.Range(0, ShaderUtil.GetPropertyCount(shader))
          .Select(shaderPropertyIndex => new ShaderProperty(
            name: ShaderUtil.GetPropertyName(shader, propertyIdx: shaderPropertyIndex),
            type: ShaderUtil.GetPropertyType(shader, propertyIdx: shaderPropertyIndex).toShaderUtilsGame()
          )).ToArray()
        : Array.Empty<ShaderProperty>();
    
    /// <returns>Return true if there was no errors.</returns>
    public static bool validateShaderPropertyName(
      IComputeFromShaderProperties computeFrom, string shaderPropertyName
    ) => computeAllShaderProperties(computeFrom).Select(_ => _.name).Contains(shaderPropertyName);
    
    /// <returns>See <see cref="validateShaderPropertyName"/>.</returns>
    public static bool validateShaderPropertyNameForType(
      IComputeFromShaderProperties computeFrom, string shaderPropertyName, ShaderPropertyAttribute.Type type
    ) => computeAllShaderPropertyNamesForType(computeFrom, type).Contains(shaderPropertyName);

    public static Option<ShaderPropertyValidationError> validateShaderProperty(
      Option<Renderer> maybeRenderer, string shaderPropertyName, ShaderPropertyAttribute.Type type
    ) {
      var maybeErrorMessage =
        !maybeRenderer.valueOut(out var renderer) ? Some.a("Renderer not found.")
        : renderer == null ? Some.a("Renderer is null.")
        : !validateShaderPropertyName(
          new ComputeFromRenderer(renderer).up, shaderPropertyName
        ) ? Some.a($"No property found with name: \"{shaderPropertyName}\", on renderer: \"{renderer.name}\".")
        : !validateShaderPropertyNameForType(
          new ComputeFromRenderer(renderer).up, shaderPropertyName, type
        ) ? Some.a(
          $"On renderer: \"{renderer.name}\" found property of name: \"{shaderPropertyName}\", " +
          $"but it was not of type: \"{type}\"."
        ) : None._;

      return maybeErrorMessage.mapM(_ => new ShaderPropertyValidationError(_));
    }
    
    public static ShaderPropertyAttribute.Type toShaderUtilsGame(
      this ShaderUtil.ShaderPropertyType shaderUtilsType
    ) => shaderUtilsType switch {
      ShaderUtil.ShaderPropertyType.Color => ShaderPropertyAttribute.Type.Color,
      ShaderUtil.ShaderPropertyType.Vector => ShaderPropertyAttribute.Type.Vector,
      ShaderUtil.ShaderPropertyType.Float or ShaderUtil.ShaderPropertyType.Range => 
        ShaderPropertyAttribute.Type.Float,
      ShaderUtil.ShaderPropertyType.TexEnv => ShaderPropertyAttribute.Type.Texture,
      _ => throw shaderUtilsType.argumentOutOfRange()
    };
  }
}

#endif