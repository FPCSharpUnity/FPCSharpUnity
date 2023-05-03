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
  static partial void showInInspectorEditorPart(GameObject gameObject, bool raycastTarget) {
    if (!raycastTarget) return;
    var withoutGraphicRaycaster = parentWithoutGraphicRaycaster(gameObject);
    var hasRayCastedChildren = getHasRayCastedChildren(gameObject.transform);
    var canvasGroupThatDisablesClicks = UserClickableBehaviourUtils.canvasGroupThatDisablesClicks(gameObject);
    var maybeErrorMsg = errorMessageIfSetupIsInvalid(
      gameObject.transform, withoutGraphicRaycaster, hasRayCastedChildren,
      componentName: "", canvasGroupThatDisablesClicks: canvasGroupThatDisablesClicks
    );
    if (maybeErrorMsg.valueOut(out var errorMsg)) {
      SirenixEditorGUI.MessageBox(errorMsg, MessageType.Error);

      {if (withoutGraphicRaycaster.valueOut(out var go)) {
        EditorGUILayout.ObjectField($"Canvas on: {go.name}", go.GetComponent<Canvas>(), typeof(Canvas), true);
        if (GUILayout.Button("Add Graphic Raycaster component")) {
          go.recordEditorChanges($"add {nameof(GraphicRaycaster)}");
          go.AddComponent<GraphicRaycaster>();       
        }        
      }}

      {if (canvasGroupThatDisablesClicks.valueOut(out var cg)) {
        EditorGUILayout.ObjectField($"CanvasGroup on: {cg.name}", cg, typeof(CanvasGroup), true);
        if (GUILayout.Button("Set CanvasGroup to block raycasts")) {
          cg.recordEditorChanges($"set blocking to true");
          cg.blocksRaycasts = true;        
        }        
      }}
    }
  }
}
#endif