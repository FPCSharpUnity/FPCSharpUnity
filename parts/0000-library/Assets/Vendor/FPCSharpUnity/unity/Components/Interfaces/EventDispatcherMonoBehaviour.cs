using System;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.reactive;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Interfaces {
  /// <summary>
  /// Base class for turning `IMB_*` interfaces into event dispatchers.
  /// </summary>
  public abstract partial class EventDispatcherMonoBehaviour<EventData> : MonoBehaviour {
    /// <summary>Reactive API.</summary>
    [PublicReadOnlyAccessor] readonly Subject<EventData> _onEvent = new();
    
    /// <summary>C# events API.</summary>
    public event Action<EventData> onEventAction;

    protected void dispatch(EventData data) {
      _onEvent.push(data);
      onEventAction?.Invoke(data);
    }
  }
}