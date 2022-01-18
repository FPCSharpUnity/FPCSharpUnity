using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnEnableForwarder : EventForwarder<Unit>, IMB_OnEnable {
    public void OnEnable() => _onEvent.push(F.unit);
  }
}