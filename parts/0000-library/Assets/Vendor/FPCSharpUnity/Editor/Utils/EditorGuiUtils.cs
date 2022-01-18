using System;
using GenerationAttributes;
using FPCSharpUnity.core.functional;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Editor.Utils {
  public static class EditorGuiUtils {
    /// <returns>True if value was changed</returns>
    public static bool toggle(string label, ref bool value) {
      EditorGUI.BeginChangeCheck();
      value = EditorGUILayout.Toggle(label, value);
      return EditorGUI.EndChangeCheck();
    }

    [SimpleMethodMacro("${value} = EditorGUILayout.Toggle(UnityEditor.ObjectNames.NicifyVariableName(\"${value}\"), ${value})")]
    public static void drawM(bool value) => throw new MacroException();
    
    [SimpleMethodMacro("${value} = EditorGUILayout.TextField(UnityEditor.ObjectNames.NicifyVariableName(\"${value}\"), ${value})")]
    public static void drawM(string value) => throw new MacroException();
    
    [SimpleMethodMacro("${value} = EditorGUILayout.IntField(UnityEditor.ObjectNames.NicifyVariableName(\"${value}\"), ${value})")]
    public static void drawM(int value) => throw new MacroException();
    
    [SimpleMethodMacro("${value} = EditorGUILayout.LongField(UnityEditor.ObjectNames.NicifyVariableName(\"${value}\"), ${value})")]
    public static void drawM(long value) => throw new MacroException();
    
    /// <returns>Some if value was changed</returns>
    public static Option<bool> toggleReturn(string label, bool value) {
      EditorGUI.BeginChangeCheck();
      var newValue = EditorGUILayout.Toggle(label, value);
      return EditorGUI.EndChangeCheck() ? Some.a(newValue) : None._;
    }
    
    public static void objectField<A>(string label, ref A value, bool allowSceneObjects = false) where A : Object {
      value = (A) EditorGUILayout.ObjectField(label, value, typeof(A), allowSceneObjects);
    }
    
    public static void foldoutBox(string label, ref bool state, Action drawContents) {
      using var _ = new GUILayout.VerticalScope(EditorStyles.helpBox);
      state = EditorGUILayout.Foldout(state, label);
      if (state) drawContents();
    }
    
    public static void box(string label, Action drawContents) {
      using var _ = new GUILayout.VerticalScope(EditorStyles.helpBox);
      GUILayout.Label(label, EditorStyles.largeLabel);
      drawContents();
    }
  }
}