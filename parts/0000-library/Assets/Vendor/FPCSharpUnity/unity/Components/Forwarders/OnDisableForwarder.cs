using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnDisableForwarder : EventForwarder<Unit>, IMB_OnDisable {
    public void OnDisable() => _onEvent.push(F.unit);
  }
}