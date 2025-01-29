﻿#nullable enable
using System.Collections.Generic;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.reactive;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components.ui;

public abstract class PointerDownUp : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {
  /// <summary>
  /// We need a list here, because:
  /// * If we made several touches and then released some of them, we need to
  ///   set isHeldDown to the last touch position we still have. Therefore we
  ///   cannot use a counter.
  /// * we can release touches in different order than we pressed. Therefore
  ///   we cannot use a stack.
  ///
  /// The amount of elements should be small (amount of simultaneous touches
  /// recognized by a device, which is usually less than 10), therefore the
  /// performance gains from switching to other data structure would be
  /// negligable here.
  /// </summary>
  protected readonly List<PointerEventData> pointerData = new();

  PointerEventData? lastPointerUpOpt;

  public readonly Subject<Unit> onExit = new();

  public void OnPointerDown(PointerEventData eventData) {
    // Theoretically for every pointer down there should be a pointer up.
    //
    // However sometimes unity dispatches two pointer downs and one pointer up.
    // So far we only noticed this behaviour in unity editor at low frame rates.
    //
    // Our hypothesis is that user clicks and releases the mouse within single frame.
    //
    // So the workaround is to double-check if this event has been dispatched by unity
    // before and ignore it if has.
    if (pointerData.Contains(eventData)) return;

    pointerData.Add(eventData);
    onPointerDown(eventData);
  }

  public void OnPointerUp(PointerEventData eventData) {
    lastPointerUpOpt = eventData;
    pointerData.Remove(eventData);
    onPointerUp(eventData);
  }

  protected abstract void onPointerDown(PointerEventData eventData);
  protected abstract void onPointerUp(PointerEventData eventData);
    
  public void OnPointerExit(PointerEventData eventData) {
    onExit.push(Unit._);
  }

  public void clearActivePointers() {
    var go = gameObject;
    foreach (var pointer in pointerData) {
      if (pointer.pointerPress == go) {
        pointer.eligibleForClick = false;
      }
    }
    if (lastPointerUpOpt != null && lastPointerUpOpt.pointerPress == go) {
      lastPointerUpOpt.eligibleForClick = false;
      lastPointerUpOpt = null;
    }
    pointerData.Clear();
  }
}