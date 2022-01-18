using FPCSharpUnity.unity.Components.Interfaces;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class TriggerEnterForwarder : EventForwarder<Collider>, IMB_OnTriggerEnter {
    public void OnTriggerEnter(Collider other) => _onEvent.push(other);
  }
}