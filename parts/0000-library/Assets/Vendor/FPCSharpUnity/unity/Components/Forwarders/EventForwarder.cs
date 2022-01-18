using FPCSharpUnity.core.reactive;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class EventForwarder<A> : MonoBehaviour {
    protected readonly Subject<A> _onEvent = new Subject<A>();
    public IRxObservable<A> onEvent => _onEvent;
  }
}