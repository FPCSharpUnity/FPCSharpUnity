using System.Linq;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
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
    this A component, out string errorMsg, Transform transform
  ) where A : Component {
    var maybeErrMsg = errorMessageIfSetupIsInvalid(
      transform, parentWithoutGraphicRaycaster(transform.gameObject),
      hasRayCastedChildren: getHasRayCastedChildren(transform),
      componentName: typeof(A).Name
    );
    errorMsg = maybeErrMsg.getOrElse("");
    return maybeErrMsg.isNone;
  }
  
  /// <summary> Validates and returns a reason text why the given component is not valid. </summary>
  static Option<string> errorMessageIfSetupIsInvalid(
    Transform transform, Option<GameObject> parentWithoutGraphicRaycaster, bool hasRayCastedChildren,
    string componentName
  ) => 
    parentWithoutGraphicRaycaster.fold(
      () => hasRayCastedChildren 
        ? None._
        : Some.a($"<b>{componentName}{transform.debugPath()}</b> will not be clickable, because it "
          + $"doesn't have any <b>{nameof(Graphic)}</b> components that are ray casted!"),
      go => Some.a($"<b>{transform.debugPath()}</b> will not be clickable, because it "
              + $"has <b>{nameof(Canvas)}</b> component placed on without <b>{nameof(GraphicRaycaster)}</b> on "
              + $"it:\n<b>{go.transform.debugPath()}</b>") 
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
    gameObject.getComponentInParents<Canvas>().flatMap(canvas => 
      // Don't check prefab-edit-mode canvases.
      canvas.name != "Canvas (Environment)" 
      && canvas.name != "Prefab Mode in Context"
      && !canvas.GetComponent<GraphicRaycaster>()
        ? Some.a(canvas.gameObject)
        : None._
    );
  
  /// <summary>
  /// Common code we use to display information message in inspector UI for component that needs to be validated.
  /// </summary>
  public static void showInInspector(GameObject gameObject) => showInInspectorEditorPart(gameObject);

  /// <summary>
  /// Common code we use to display information message in inspector UI for component that needs to be validated.
  /// It's implemented in partial file and has #if UNITY_EDITOR.
  /// </summary>
  static partial void showInInspectorEditorPart(GameObject gameObject);
}