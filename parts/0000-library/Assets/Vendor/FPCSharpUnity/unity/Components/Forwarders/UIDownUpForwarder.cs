using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Components.ui;
using GenerationAttributes;
using FPCSharpUnity.core.reactive;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components {
  public partial class UIDownUpForwarder : PointerDownUp, IMB_OnDisable {
    [Record] public readonly partial struct OnUpData {
      public enum UpType : byte { OnPointerUp, OnDisable }

      public readonly PointerEventData eventData;
      public readonly UpType type;
    }

    readonly Subject<PointerEventData> _onDown = new();
    readonly Subject<OnUpData> _onUp = new();
    public IRxObservable<PointerEventData> onDown => _onDown;
    public IRxObservable<OnUpData> onUp => _onUp;
    public bool isDown { get; private set; }

    void up(OnUpData eventData) {
      _onUp.push(eventData);
      isDown = false;
    }

    protected override void onPointerDown(PointerEventData eventData) {
      if (isActiveAndEnabled) {
        _onDown.push(eventData);
        isDown = true;
      }
    }

    protected override void onPointerUp(PointerEventData eventData) {
      if (isActiveAndEnabled)
        up(new OnUpData(eventData, OnUpData.UpType.OnPointerUp));
    }

    public void OnDisable() {
      if (isDown) {
        foreach (var data in pointerData) {
          up(new OnUpData(data, OnUpData.UpType.OnDisable));
        }
      }
    }
  }
}
