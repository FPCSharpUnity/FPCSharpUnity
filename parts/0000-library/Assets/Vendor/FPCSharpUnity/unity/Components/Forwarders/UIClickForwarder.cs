using System;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;

using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components {
  public class UIClickForwarder : UIBehaviour, IPointerClickHandler {
    readonly Subject<Unit> _onClick = new();
    public IRxObservable<Unit> onClick => _onClick;

    public void OnPointerClick(PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left && IsActive())
        _onClick.push(F.unit);
    }
  }

  [Serializable, PublicAPI] public class UIClickForwarderPrefab : TagPrefab<UIClickForwarder> {}
}
