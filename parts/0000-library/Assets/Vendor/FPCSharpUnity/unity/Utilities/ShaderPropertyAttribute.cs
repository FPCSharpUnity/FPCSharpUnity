using System;
using UnityEngine;

namespace FPCSharpUnity.unity.core.Utilities {
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class ShaderPropertyAttribute : Attribute {
    /// <summary>
    /// Name of method that should evaluate to target <see cref="Renderer"/>.
    /// From where shader properties will be looked at.
    /// </summary>
    public string rendererGetter;

    /// <summary>
    /// Gets list of properties of type.
    /// </summary>
    public ShaderUtilsGame.ShaderPropertyType forType;

    public ShaderPropertyAttribute(string rendererGetter, ShaderUtilsGame.ShaderPropertyType forType) {
      this.rendererGetter = rendererGetter;
      this.forType = forType;
    }
  }
}