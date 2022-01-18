using FPCSharpUnity.unity.Components.Forwarders;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components {
  /**
   * UpdateForwarder addresses an issue where ASync.EveryFrame creates a new
   * coroutine and drops it if GameObject is disabled.
   * UpdateForwarder does not drop subscriptions if GameObject is disabled.
   */
  public class UpdateForwarder : EventForwarder<Unit>, IMB_Update {
    public void Update() => _onEvent.push(F.unit);
  }
}
