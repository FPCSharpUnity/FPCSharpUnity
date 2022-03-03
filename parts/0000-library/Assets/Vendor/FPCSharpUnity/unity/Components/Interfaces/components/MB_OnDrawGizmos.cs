using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components.Interfaces {
  public class MB_OnDrawGizmos : EventDispatcherMonoBehaviour<Unit>, IMB_OnDrawGizmos {
    public void OnDrawGizmos() => dispatch(Unit._);
  }
}