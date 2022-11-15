using UnityEngine;

namespace FPCSharpUnity.unity.Extensions; 

public static class StringExtsU {
  /// <summary>
  /// Returns the syntax for Unity text handlers to color the <see cref="content"/>.
  /// <para/>
  /// See https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html#supported-tags for more info.
  /// </summary>
  public static string wrapInColorTag(this string content, Color color) =>
    $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{content}</color>";
  
  /// <summary>
  /// Returns the syntax for Unity text handlers to size the <see cref="content"/>.
  /// <para/>
  /// See https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html#supported-tags for more info.
  /// </summary>
  public static string wrapInSizeTag(this string content, string size) =>
    $"<size=#{size}>{content}</size>";
}