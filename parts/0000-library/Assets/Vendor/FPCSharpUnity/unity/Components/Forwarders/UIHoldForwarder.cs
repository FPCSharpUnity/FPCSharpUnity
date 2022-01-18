using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Components.ui;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;

using FPCSharpUnity.core.functional;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components {
  public class UIHoldForwarder : PointerDownUp, IMB_Update {
    readonly IRxRef<Option<Vector2>> _isHeldDown = RxRef.a(F.none<Vector2>());
    public IRxVal<Option<Vector2>> isHeldDown => _isHeldDown;

    readonly Subject<Vector2> _onHoldEveryFrame = new Subject<Vector2>();
    public IRxObservable<Vector2> onHoldEveryFrame => _onHoldEveryFrame;

    public void Update() {
      if (pointerData.isEmpty()) return;

      // HOT CODE: We explicitly use indexing instead of LINQ .Last() to make
      // sure this performs well and does not do unnecessary checks.
      var lastPointer = pointerData[pointerData.Count - 1];
      _onHoldEveryFrame.push(lastPointer.position);
    }

    protected override void onPointerDown(PointerEventData eventData) {
      _isHeldDown.value = eventData.position.some();
    }

    protected override void onPointerUp(PointerEventData eventData) {
      if (pointerData.isEmpty()) {
        _isHeldDown.value = None._;
      }
    }
  }
}