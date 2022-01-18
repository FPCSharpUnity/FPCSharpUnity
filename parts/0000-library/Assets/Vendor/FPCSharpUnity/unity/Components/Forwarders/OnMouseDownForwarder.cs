using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnMouseDownForwarder : EventForwarder<Unit>, IMB_OnMouseDown {
    public void OnMouseDown() => _onEvent.push(F.unit);
  }
}
