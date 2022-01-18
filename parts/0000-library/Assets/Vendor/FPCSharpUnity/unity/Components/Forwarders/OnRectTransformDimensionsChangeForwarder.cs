using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;

using FPCSharpUnity.core.functional;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components.Forwarders {
  public class OnRectTransformDimensionsChangeForwarder : UIBehaviour {
    readonly Subject<Unit> _rectDimensionsChanged = new Subject<Unit>();

    public IRxObservable<Unit> rectDimensionsChanged => _rectDimensionsChanged;
    public RectTransform rectTransform => (RectTransform) transform;

    protected override void OnRectTransformDimensionsChange() => _rectDimensionsChanged.push(F.unit);
  }
}