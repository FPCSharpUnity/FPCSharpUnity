using System.Linq;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.pools;
using FPCSharpUnity.unity.Extensions;
using UnityEngine.UI;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

/// <summary>
/// Common code for clickable UI elements validation.
/// </summary>
public static partial class UserClickableBehaviourUtils {
  /// <summary> Validates whether this button can be clicked by the user. </summary>
  /// <returns>Whether the component is valid.</returns>
  public static bool validateIfThisWillBeClickable<A>(
    this A component, bool raycastTarget, out string errorMsg
  ) where A : Component {
    if (!raycastTarget) {
      errorMsg = "";
      return true;
    }
    var transform = component.transform;
    var maybeErrMsg = errorMessageIfSetupIsInvalid(
      transform, parentWithoutGraphicRaycaster(transform.gameObject),
      hasRayCastedChildren: getHasRayCastedChildren(transform),
      componentName: typeof(A).Name,
      canvasGroupThatDisablesClicks: canvasGroupThatDisablesClicks(transform.gameObject)
    );
    errorMsg = maybeErrMsg.getOrElse("");
    return maybeErrMsg.isNone;
  }
  
  /// <summary> Validates and returns a reason text why the given component is not valid. </summary>
  static Option<string> errorMessageIfSetupIsInvalid(
    Transform transform, Option<GameObject> parentWithoutGraphicRaycaster, bool hasRayCastedChildren,
    string componentName, Option<CanvasGroup> canvasGroupThatDisablesClicks
  ) => 
    canvasGroupThatDisablesClicks.foldM(
      () => parentWithoutGraphicRaycaster.foldM(
        () => hasRayCastedChildren 
          ? None._
          : Some.a($"<b>{componentName}{transform.debugPath()}</b> will not be clickable, because it "
                   + $"doesn't have any <b>{nameof(Graphic)}</b> components that are ray casted!"),
        go => Some.a($"<b>{transform.debugPath()}</b> will not be clickable, because it "
                     + $"has <b>{nameof(Canvas)}</b> component placed on without <b>{nameof(GraphicRaycaster)}</b> on "
                     + $"it:\n<b>{go.transform.debugPath()}</b>") 
      ),
      canvasGroup => Some.a($"<b>{transform.debugPath()}</b> will not be clickable, because it "
                            + $"has <b>{nameof(CanvasGroup)}</b> component that disables clicks:\n"
                            + $"<b>{canvasGroup.transform.debugPath()}</b>")
    );

  /// <summary> Checks if <see cref="transform"/>'s game object can be raycasted. </summary>
  static bool getHasRayCastedChildren(Transform transform) =>
    transform.GetComponentsInChildren<Graphic>(includeInactive: true).Concat(transform.GetComponents<Graphic>())
      .Any(_ => _.raycastTarget);
  
  /// <summary>
  /// Finds a <see cref="Canvas"/> component on parent which doesn't have <see cref="GraphicRaycaster"/> component
  /// along side it. This means that all children of this parent will not be raycasted.
  /// </summary>
  static Option<GameObject> parentWithoutGraphicRaycaster(GameObject gameObject) =>
    gameObject.getComponentInParents<Canvas>().flatMapM(canvas => 
      // Don't check prefab-edit-mode canvases.
      canvas.name != "Canvas (Environment)" 
      && canvas.name != "Prefab Mode in Context"
      && !canvas.GetComponent<GraphicRaycaster>()
        ? Some.a(canvas.gameObject)
        : None._
    );
  
  /// <summary>
  /// This method is a bit complicated, but it's because of the way Unity handles CanvasGroup.
  /// If you have a parent CanvasGroup that has blocksRaycasts=false all children UI will not be raycasted.
  /// this method finds these CanvasGroups and returns the first one that has blocksRaycasts=false.
  /// <para/>
  /// All possible states example:
  /// <code><![CDATA[
  /// Parent state.
  ///    Child state.  -> Result
  ///.
  /// blocksRaycasts=true 
  ///    blocksRaycasts=true, ignoreParentGroups = false   ->  OK
  ///.
  /// blocksRaycasts=true
  ///    blocksRaycasts=true, ignoreParentGroups = true   ->  OK
  ///.
  /// blocksRaycasts=true
  ///    blocksRaycasts=false, ignoreParentGroups = true   ->  BAD
  ///.
  /// blocksRaycasts=true
  ///    blocksRaycasts=false, ignoreParentGroups = false   ->  OK
  ///.
  ///.
  /// blocksRaycasts=false 
  ///    blocksRaycasts=true, ignoreParentGroups = false   ->  OK
  ///.
  /// blocksRaycasts=false
  ///    blocksRaycasts=true, ignoreParentGroups = true   ->  OK
  ///.
  /// blocksRaycasts=false
  ///    blocksRaycasts=false, ignoreParentGroups = true   ->  BAD
  ///.
  /// blocksRaycasts=false
  ///    blocksRaycasts=false, ignoreParentGroups = false   ->  BAD
  /// ]]></code>
  /// </summary>
  static Option<CanvasGroup> canvasGroupThatDisablesClicks(GameObject gameObject) {
    var tr = gameObject.transform;
    var prevCanvasGroup = Option<CanvasGroup>.None;
    while (tr != null) {
      if (tr.TryGetComponent<CanvasGroup>(out var canvasGroup) && canvasGroup.enabled) {
        if (canvasGroup.blocksRaycasts) return None._;
        if (canvasGroup.ignoreParentGroups) return Some.a(canvasGroup);
        prevCanvasGroup = Some.a(canvasGroup);
      }
      tr = tr.parent;
    }
    return prevCanvasGroup;
  }
  
  /// <summary>
  /// Common code we use to display information message in inspector UI for component that needs to be validated.
  /// </summary>
  public static void showInInspector(GameObject gameObject) => showInInspectorEditorPart(gameObject, raycastTarget: true);
  
  /// <summary>
  /// Common code we use to display information message in inspector UI for component that needs to be validated.
  /// </summary>
  public static void showInInspector(Graphic graphic) => 
    showInInspectorEditorPart(graphic.gameObject, graphic.raycastTarget);

  /// <summary>
  /// Common code we use to display information message in inspector UI for component that needs to be validated.
  /// It's implemented in partial file and has #if UNITY_EDITOR.
  /// </summary>
  static partial void showInInspectorEditorPart(GameObject gameObject, bool raycastTarget);
}