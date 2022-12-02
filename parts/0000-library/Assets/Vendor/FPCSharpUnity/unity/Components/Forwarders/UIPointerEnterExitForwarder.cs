using FPCSharpUnity.core.reactive;

using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.unity.Concurrent;
using GenerationAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components {
  public partial class UIPointerEnterExitForwarder : UIBehaviour, IPointerEnterHandler, IPointerExitHandler {
    /// <summary>Emits an event when pointer enters this <see cref="GameObject"/>.</summary>
    [PublicReadOnlyAccessor] readonly Subject<Unit> _onEnter = new Subject<Unit>();
  
    /// <summary>Emits an event when pointer exists this <see cref="GameObject"/>.</summary>
    [PublicReadOnlyAccessor] readonly Subject<Unit> _onExit = new Subject<Unit>();
  
    /// <summary>Returns true if pointer is on this <see cref="GameObject"/>.</summary>
    public bool isEntered { get; private set; }

    /// <summary>
    /// Emits events every frame on update while pointer is on this <see cref="GameObject"/>.
    /// </summary>
    [LazyProperty] public IRxObservable<Unit> onHover => ASync.onUpdate.filter(_ => isEntered);

    public void OnPointerEnter(PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left && IsActive()) {
        _onEnter.push(new Unit());
        isEntered = true;
      }
    }

    public void OnPointerExit(PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left && IsActive()) {
        _onExit.push(new Unit());
        isEntered = false;
      }
    }
  }
}