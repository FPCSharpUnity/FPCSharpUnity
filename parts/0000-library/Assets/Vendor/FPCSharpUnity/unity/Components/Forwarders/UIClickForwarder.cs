using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.macros;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components {
  public partial class UIClickForwarder : UIBehaviour, IPointerClickHandler {
    [PublicReadOnlyAccessor] readonly Subject<Unit> _onClick = new();

    public void OnPointerClick(PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left && IsActive())
        _onClick.push(F.unit);
    }
  }
}
