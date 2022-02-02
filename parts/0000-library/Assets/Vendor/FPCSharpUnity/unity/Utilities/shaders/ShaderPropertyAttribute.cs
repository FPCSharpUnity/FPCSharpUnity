using System;
using UnityEngine;
using UnityEngine.Rendering;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.core.Utilities {
  /// <summary>
  /// When attached to a string field marks the string as a property name in a shader.
  /// <para/>
  /// This does two things: it replaces the Unity inspector drawer from a simple string drawer to a
  /// dropdown that shows you the properties of the specified <see cref="forType" /> and also adds
  /// validation with our object validator to make sure the shader property name is valid.
  /// </summary>
  /// <example><code><![CDATA[
  /// [
  ///   SerializeField, 
  ///   ShaderProperty(
  ///     rendererGetter: nameof(getRenderer), 
  ///     ShaderPropertyAttribute.Type.Float
  ///   )
  /// ] string _shaderProperty;
  ///
  /// Renderer getRenderer() => _target;
  /// ]]></code></example>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false), PublicAPI]
  public class ShaderPropertyAttribute : Attribute {
    /// <summary>
    /// Name of the method that should evaluate to the target <see cref="Renderer"/> from where available 
    /// shader properties will be looked up from.
    /// </summary>
    public readonly string rendererGetter;

    /// <summary>Specifies what type of shader property this property name is referencing to.</summary>
    public readonly Type forType;

    public ShaderPropertyAttribute(string rendererGetter, Type forType) {
      this.rendererGetter = rendererGetter;
      this.forType = forType;
    }

    /// <summary>
    /// Mirrors <see cref="ShaderPropertyType"/> enum, except it does not have the 
    /// <see cref="ShaderPropertyType.Range"/>, because from the setting-values API standpoint, which exists
    /// in Unity player runtime, you can not distinguish between the <see cref="ShaderPropertyType.Range"/> 
    /// and <see cref="ShaderPropertyType.Float"/> when calling
    /// <see cref="MaterialPropertyBlock.SetFloat(string,float)"/>.
    /// </summary>
    [PublicAPI] public enum Type : byte {
      /// <inheritdoc cref="ShaderPropertyType.Color"/>
      Color,
      /// <inheritdoc cref="ShaderPropertyType.Vector"/>
      Vector,
      /// <inheritdoc cref="ShaderPropertyType.Float"/>
      Float,
      /// <inheritdoc cref="ShaderPropertyType.Texture"/>
      Texture
    }
  }
}