#if UNITY_EDITOR
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui; 

public static partial class UserClickableBehaviourUtils {
  static partial void showInInspectorEditorPart(GameObject gameObject) {
    var withoutGraphicRaycaster = parentWithoutGraphicRaycaster(gameObject);
    var hasRayCastedChildren = getHasRayCastedChildren(gameObject.transform);
    var maybeErrorMsg = errorMessageIfSetupIsInvalid(gameObject.transform, withoutGraphicRaycaster, hasRayCastedChildren,
      componentName: ""
    );
    if (maybeErrorMsg.valueOut(out var errorMsg)) {
      SirenixEditorGUI.MessageBox(errorMsg, MessageType.Error);

      {if (withoutGraphicRaycaster.valueOut(out var go)) {
        EditorGUILayout.ObjectField($"Canvas on: {go.name}", go.GetComponent<Canvas>(), typeof(Canvas));
      }}
      
      if (withoutGraphicRaycaster.isSome && GUILayout.Button("Fix Graphic Raycasters")) {
        foreach (var rt in withoutGraphicRaycaster) {
          rt.gameObject.recordEditorChanges($"add {nameof(GraphicRaycaster)}");
          rt.gameObject.AddComponent<GraphicRaycaster>();
        }          
      }
    }
  }
}
#endif