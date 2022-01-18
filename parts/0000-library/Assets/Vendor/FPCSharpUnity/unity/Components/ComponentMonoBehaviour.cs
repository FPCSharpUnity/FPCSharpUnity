using UnityEngine;

namespace FPCSharpUnity.unity.Components {
  public abstract class ComponentMonoBehaviour : MonoBehaviour {
    protected virtual void Reset() {
      // we may want to set hide flags here
    }
  }
}