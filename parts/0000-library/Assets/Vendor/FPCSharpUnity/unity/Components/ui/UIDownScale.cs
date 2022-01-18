using FPCSharpUnity.unity.Components.animations;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.functional;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components.ui {
  public class UIDownScale : PointerDownUp, IMB_Awake {
    public float scale = 1.1f;
    [Header("Optional, default to this.transform")]
    public Transform target;

    protected Vector3 previous;
    protected bool isDown;
    protected new Option<SinusoidScale> animation;

    public virtual Vector3 targetLocalScale {
      get { return target.localScale; }
      set { target.localScale = value; }
    }

    public void Awake() {
      if (!target) target = transform;
      animation = target.gameObject.GetComponentOption<SinusoidScale>();
    }

    protected override void onPointerDown(PointerEventData eventData) {
      if (eventData.button != PointerEventData.InputButton.Left) return;
      pointerDown();
    }

    public void pointerDown() {
      if (!isDown) {
        isDown = true;
        previous = targetLocalScale;
        targetLocalScale *= scale;
        foreach (var anim in animation) anim.enabled = !isDown;
      }
    }

    protected override void onPointerUp(PointerEventData eventData) {
      if (eventData.button != PointerEventData.InputButton.Left) return;
      pointerUp();
    }

    public void pointerUp() {
      if (isDown) {
        isDown = false;
        targetLocalScale = previous;
        foreach (var anim in animation) anim.enabled = !isDown;
        onPointerUp();
      }
    }

    public virtual void onPointerUp() {}
  }
}
