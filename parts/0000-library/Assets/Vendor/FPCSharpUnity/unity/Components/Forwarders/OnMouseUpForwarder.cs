using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnMouseUpForwarder : EventForwarder<Unit>, IMB_OnMouseUp {
    public void OnMouseUp() => _onEvent.push(F.unit);
  }
}
