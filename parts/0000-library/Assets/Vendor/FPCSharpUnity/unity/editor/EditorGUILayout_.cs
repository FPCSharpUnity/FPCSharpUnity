#if UNITY_EDITOR
using System;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.editor {
  /// <summary>
  /// Extensions for <see cref="EditorGUILayout"/>.
  ///
  /// This is in non-editor assembly because sometimes you want to access this code from your main game assembly as
  /// well. 
  /// </summary>
  [PublicAPI] public static class EditorGUILayout_ {
    // Unfortunately macros don't support params at the moment.
    
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
  }
}
#endif