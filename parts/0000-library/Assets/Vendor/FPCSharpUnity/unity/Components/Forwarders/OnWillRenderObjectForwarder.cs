using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnWillRenderObjectForwarder : EventForwarder<Camera>, IMB_OnWillRenderObject {
    public void OnWillRenderObject() {
      if (Log.d.isVerbose()) Log.d.verbose(
        $"{nameof(OnWillRenderObjectForwarder)} this = {this}, camera current = {Camera.current}"
      );
      _onEvent.push(Camera.current);
    }
  }
}