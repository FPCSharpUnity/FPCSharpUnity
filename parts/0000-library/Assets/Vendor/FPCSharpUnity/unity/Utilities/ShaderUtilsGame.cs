using FPCSharpUnity.core.exts;
using UnityEditor;

namespace FPCSharpUnity.unity.core.Utilities {
  public static class ShaderUtilsGame {
    /// <summary>
    /// We create a subset of <see cref="ShaderUtil.ShaderPropertyType"/>
    /// Which maps <see cref="ShaderUtil.ShaderPropertyType.Range"/> and <see cref="ShaderUtil.ShaderPropertyType.Float"/>
    /// to <see cref="ShaderPropertyType.Float"/> 
    /// </summary>
    public enum ShaderPropertyType : byte {
      Color,
      Vector,
      Float,
      Texture
    }

    public static ShaderPropertyType fromShaderUtils(
      this ShaderUtil.ShaderPropertyType shaderUtilsType
    ) => shaderUtilsType switch {
      ShaderUtil.ShaderPropertyType.Color => ShaderPropertyType.Color,
      ShaderUtil.ShaderPropertyType.Vector => ShaderPropertyType.Vector,
      ShaderUtil.ShaderPropertyType.Float => ShaderPropertyType.Float,
      ShaderUtil.ShaderPropertyType.Range => ShaderPropertyType.Float,
      ShaderUtil.ShaderPropertyType.TexEnv => ShaderPropertyType.Texture,
      _ => throw shaderUtilsType.argumentOutOfRange()
    };
  }
}