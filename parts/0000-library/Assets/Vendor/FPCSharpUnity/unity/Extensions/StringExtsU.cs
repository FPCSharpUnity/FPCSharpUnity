using UnityEngine;

namespace FPCSharpUnity.unity.Extensions; 

public static class StringExtsU {
  public static string wrapInColorTag(this string content, Color color) => 
    wrapInColorTag(content, ColorUtility.ToHtmlStringRGBA(color));
  
  public static string wrapInColorTag(this string content, string color) => 
    $"<color=#{color}>{content}</color>";
}