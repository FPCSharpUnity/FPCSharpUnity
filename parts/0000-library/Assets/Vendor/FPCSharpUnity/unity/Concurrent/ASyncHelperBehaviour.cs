using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;

using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.macros;
using UnityEngine;

namespace FPCSharpUnity.unity.Concurrent {
  public partial class ASyncHelperBehaviour : MonoBehaviour,
    IMB_OnApplicationPause, IMB_OnApplicationQuit, IMB_LateUpdate, IMB_Update, IMB_OnApplicationFocus
  {
    [PublicReadOnlyAccessor] readonly Subject<bool> _onPause = new();
    public void OnApplicationPause(bool paused) => _onPause.push(paused);

    [PublicReadOnlyAccessor] readonly Subject<Unit> _onQuit = new();
    public void OnApplicationQuit() => _onQuit.push(F.unit);

    [PublicReadOnlyAccessor] readonly Subject<Unit> _onLateUpdate = new();
    public void LateUpdate() => _onLateUpdate.push(F.unit);
    
    [PublicReadOnlyAccessor] readonly Subject<Unit> _onUpdate = new();
    public void Update() => _onUpdate.push(F.unit);
    
    [PublicReadOnlyAccessor] readonly IRxRef<bool> _hasFocus = RxRef.a(true);
    public void OnApplicationFocus(bool focus) => _hasFocus.value = focus;
  }

  // Don't implement unity interfaces if not used
  class ASyncHelperBehaviourEmpty : MonoBehaviour {}
}
