﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components {
  /// <summary>
  /// Allows to receive UI click events even if we do not have a visible part for this UI object.
  /// </summary>
  [RequireComponent(typeof(CanvasRenderer))]
  public class InvisibleUIClickEventsReceiver : Graphic {
    InvisibleUIClickEventsReceiver() => useLegacyMeshGeneration = false;

    // http://forum.unity3d.com/threads/recttransform-and-events.285740/
    // Do not generate mesh (do not call base).
    [Obsolete] protected override void OnFillVBO(List<UIVertex> vbo) {}

#if !UNITY_5_1 && !UNITY_5_0
    [Obsolete] protected override void OnPopulateMesh(Mesh m) => m.Clear();
    protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
#endif
  }
}
