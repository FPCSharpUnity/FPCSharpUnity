using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnDestroyForwarder : MonoBehaviour, IMB_OnDestroy {
    readonly Promise<Unit> _onDestroy;
    [PublicAPI] public readonly Future<Unit> onEvent;

    OnDestroyForwarder() {
      onEvent = Future.async<Unit>(out _onDestroy);
    }

    public void OnDestroy() => _onDestroy.complete(F.unit);
  }
}