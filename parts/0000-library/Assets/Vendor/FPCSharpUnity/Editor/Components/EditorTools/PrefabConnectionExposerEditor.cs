using System;
using System.Collections.Generic;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.EditorTools {
  [CustomEditor(typeof(PrefabConnectionExposer))]
  public class PrefabConnectionExposerEditor : UnityEditor.Editor {
    public override void OnInspectorGUI() {
      var so = (PrefabConnectionExposer) serializedObject.targetObject;

      var instanceTransform = so.instance.transform;
      var prefabTransform = so.prefab.transform;

      void run<A>(
        string name, Func<Transform, A> get, Action<Transform, A> set
      ) {
        var instanceV = get(instanceTransform);
        var prefabV = get(prefabTransform);
        var same = EqualityComparer<A>.Default.Equals(instanceV, prefabV);
        EditorGUILayout.LabelField($"{name} in prefab", same ? "same" : prefabV.ToString());
        if (!same) {
          if (GUILayout.Button($"Apply current {name.ToLower()} to prefab")) {
            set(prefabTransform, instanceV);
            if (Log.d.isDebug())
              Log.d.debug($"{name} set to {instanceV} (was {prefabV}) on prefab.", so.prefab);
          }
        }
      }

      if (GUILayout.Button("Select prefab")) {
        Selection.activeObject = so.prefab;
      }
      GUILayoutUtility.GetRect(6f, 24f);
      
      run("Position", _ => _.localPosition, (t, v) => t.localPosition = v);
      GUILayoutUtility.GetRect(6f, 24f);
      run("Rotation", _ => _.localRotation, (t, v) => t.localRotation = v);
      GUILayoutUtility.GetRect(6f, 24f);
      run("Scale", _ => _.localScale, (t, v) => t.localScale = v);
    }
  }
}