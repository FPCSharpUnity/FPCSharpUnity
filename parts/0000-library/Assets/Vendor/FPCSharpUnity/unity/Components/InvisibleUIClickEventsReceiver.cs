using System;
using System.Collections.Generic;
using FPCSharpUnity.unity.Components.ui;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Components {
  /// <summary>
  /// Allows to receive UI click events even if we do not have a visible part for this UI object.
  /// </summary>
  [RequireComponent(typeof(CanvasRenderer))]
  public class InvisibleUIClickEventsReceiver : Graphic, OnObjectValidate {
    InvisibleUIClickEventsReceiver() => useLegacyMeshGeneration = false;

    // http://forum.unity3d.com/threads/recttransform-and-events.285740/
    // Do not generate mesh (do not call base).
    [Obsolete] protected override void OnFillVBO(List<UIVertex> vbo) {}
    
    /// <inheritdoc cref="UserClickableBehaviourUtils.showInInspector"/>
    [OnInspectorGUI] void fixRayCastersGui() => UserClickableBehaviourUtils.showInInspector(gameObject);


#if !UNITY_5_1 && !UNITY_5_0
    [Obsolete] protected override void OnPopulateMesh(Mesh m) => m.Clear();
    protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
#endif
    
    public bool onObjectValidateIsThreadSafe => false;
    
    public IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent) {
      if (!this.validateIfThisWillBeClickable(out var errorMsg, transform)) yield return new ErrorMsg(errorMsg);
    }
  }
}
