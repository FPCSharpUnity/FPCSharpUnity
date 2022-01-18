using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnGUIForwarder : EventForwarder<Unit>, IMB_OnGUI {
    public void OnGUI() => _onEvent.push(F.unit);
  }
}