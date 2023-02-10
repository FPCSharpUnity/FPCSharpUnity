#if UNITY_EDITOR
using System;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.editor;
/// <summary>
/// Extensions for <see cref="EditorGUILayout"/>.
/// <para/>
/// This is in non-editor assembly because sometimes you want to access this code from your main game assembly as
/// well. 
/// </summary>
[PublicAPI] public static class EditorGUILayout_ {
  // Unfortunately macros don't support ref params at the moment.
  
  /// <summary>Derives name from the expression.</summary>
  [SimpleMethodMacro(@"UnityEditor.EditorGUILayout.FloatField(nameof(${a}), ${a})")]
  public static float FloatField(float a) => throw new MacroException();
  
  /// <summary>Derives name from the expression.</summary>
  [SimpleMethodMacro(@"UnityEditor.EditorGUILayout.FloatField(nameof(${a}), ${a}, ${options})")]
  public static float FloatField(float a, GUILayoutOption[] options) => throw new MacroException();
  
  /// <summary>Derives name from the expression.</summary>
  [SimpleMethodMacro(@"UnityEditor.EditorGUILayout.Toggle(nameof(${a}), ${a})")]
  public static bool Toggle(bool a) => throw new MacroException();
  
  /// <summary>Derives name from the expression.</summary>
  [SimpleMethodMacro(@"UnityEditor.EditorGUILayout.Toggle(nameof(${a}), ${a}, ${options})")]
  public static bool Toggle(bool a, GUILayoutOption[] options) => throw new MacroException();
  
  /// <summary>Derives name from the expression.</summary>
  [SimpleMethodMacro(@"FPCSharpUnity.unity.editor.EditorGUILayout_.EnumPopup(nameof(${a}), ${a})")]
  public static A EnumPopup<A>(A a) where A : Enum => throw new MacroException();
  
  /// <summary>Derives name from the expression.</summary>
  [SimpleMethodMacro(@"FPCSharpUnity.unity.editor.EditorGUILayout_.EnumPopup(nameof(${a}), ${a}, ${options})")]
  public static A EnumPopup<A>(A a, GUILayoutOption[] options) where A : Enum => throw new MacroException();

  public static A EnumPopup<A>(string label, A a, params GUILayoutOption[] options) where A : Enum =>
    (A) EditorGUILayout.EnumPopup(label, a, options);

  /// <summary>Style for <see cref="ToggleButton"/> which is not active.</summary>
  [LazyProperty] public static GUIStyle ToggleButtonStyleNotPressed => 
    // https://gamedev.stackexchange.com/a/98924
    new GUIStyle("ToolbarButton");
  
  /// <summary>Style for <see cref="ToggleButton"/> which is active.</summary>
  [LazyProperty] public static GUIStyle ToggleButtonStylePressed {
    get {
      var baseStyle = ToggleButtonStyleNotPressed;
      // This shit should change the button style, but it does not seem to work :(
      // 
      // I've spent a good hour on this already, so I just give up.
      var style = new GUIStyle(baseStyle) {
        fontStyle = FontStyle.BoldAndItalic,
        normal = baseStyle.active,
        onNormal = baseStyle.onActive
      };
      return style;
    }
  }

  /// <summary>Renders a toggleable button.</summary>
  public static bool ToggleButton(
    ref bool currentState, string label, string tooltip=null, params GUILayoutOption[] options
  ) {
    var style = currentState ? ToggleButtonStylePressed : ToggleButtonStyleNotPressed;
    var pressed = GUILayout.Button(new GUIContent(text: label, tooltip: tooltip), style, options);
    if (pressed) currentState = !currentState;
    return pressed;
  }

  /// <summary>
  /// As <see cref="GUILayout.Button(string,UnityEngine.GUILayoutOption[])"/> but respects
  /// <see cref="EditorGUI.indentLevel"/>.
  /// </summary>
  /// <note>
  /// Courtesy of https://forum.unity.com/threads/indenting-guilayout-objects.113494/#post-3350520
  /// </note>
  public static bool Button(string text) {
    var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
    return GUI.Button(rect, text);
  }
}
#endif