﻿using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnRenderObjectForwarder : EventForwarder<Camera>, IMB_OnRenderObject {
    public void OnRenderObject() {
      if (Log.d.isVerbose()) Log.d.verbose(
        $"{nameof(OnRenderObjectForwarder)} this = {this}, camera current = {Camera.current}"
      );
      _onEvent.push(Camera.current);
    }
  }
}