using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;

using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Concurrent {
  public class ASyncHelperBehaviour : MonoBehaviour,
    IMB_OnApplicationPause, IMB_OnApplicationQuit, IMB_LateUpdate, IMB_Update
  {
    readonly Subject<bool> _onPause = new();
    public IRxObservable<bool> onPause => _onPause;
    public void OnApplicationPause(bool paused) => _onPause.push(paused);

    readonly Subject<Unit> _onQuit = new();
    public IRxObservable<Unit> onQuit => _onQuit;
    public void OnApplicationQuit() => _onQuit.push(F.unit);

    readonly Subject<Unit> _onLateUpdate = new();
    public IRxObservable<Unit> onLateUpdate => _onLateUpdate;
    public void LateUpdate() => _onLateUpdate.push(F.unit);
    
    readonly Subject<Unit> _onUpdate = new();
    public IRxObservable<Unit> onUpdate => _onLateUpdate;
    public void Update() => _onUpdate.push(F.unit);
  }

  // Don't implement unity interfaces if not used
  class ASyncHelperBehaviourEmpty : MonoBehaviour {}
}
