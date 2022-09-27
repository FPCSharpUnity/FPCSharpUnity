﻿using System;
using System.Collections.Generic;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Components.Swiping;
using FPCSharpUnity.unity.Data.units;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Reactive;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.unity.unity_serialization;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  public partial class Carousel : Carousel<CarouselGameObject> {
    public enum Direction : byte { Horizontal = 0, Vertical = 1 }
    
    [Record]
    public partial struct Pages {
      [PublicAPI] public readonly float pages;
      
      [PublicAPI] public static Pages a(float pages) => new Pages(pages);
      
      public static Pages operator +(Pages a1, float a2) => new Pages(a1.pages + a2);
      public static Pages operator -(Pages a1, float a2) => new Pages(a1.pages - a2);

      public static implicit operator float(Pages pages) => pages.pages;
    }
  }

  public interface ICarouselItem {
    GameObject gameObject { get; }
  }

  public partial class Carousel<A> : UIBehaviour, IMB_Update, IMB_OnDrawGizmosSelected, IMB_OnValidate where A : ICarouselItem {
    [Record(ConstructorFlags.Withers)]
    public readonly partial struct ResizableRectData {
      [PublicAPI] public readonly Transform firstVisibleItem, lastVisibleItem;
      [PublicAPI] public readonly RectTransform target;
    }


    #region Unity Serialized Fields

#pragma warning disable 649
    public float
      SpaceBetweenSelectedAndAdjacentPages,
      SpaceBetweenOtherPages,
      SelectedPageItemsScale = 1,
      OtherPagesItemsScale = 1,
      AdjacentToSelectedPageItemsScale = 1,
      moveCompletedEventThreshold = 0.02f;
    public bool wrapCarouselAround;
    bool wrapableAround => wrapCarouselAround;
    [
      SerializeField,
      InfoBox(
        "If wraparound is enabled, how many elements do we have to have in the carousel " +
        "to start wrapping around? This is needed, because if, for example, we only have 2 " +
        "elements and they fit into the screen, user can see the wraparound moving the elements " +
        "in the view."
      ),
      // Inspect(nameof(wrapableAround))
    ] int _minElementsForWraparound = 5;
    [SerializeField] protected UnityOptionInt maxElementsFromCenter;
    [SerializeField] UnityOptionVector3 selectedPageOffset;
    // ReSharper disable once NotNullMemberIsNotInitialized
    [SerializeField] Carousel.Direction _direction = Carousel.Direction.Horizontal;

    [
      SerializeField,
      InfoBox(
        "If an item on the left side exceeds specified distance, it will be put on the right side.\n\n" +
        "1 unit means 1 item. Eg.: if you want to have zero items on the left side, you can set this value to 0.5. " +
        "When animating, the element will be on the left side for half of a single movement animation and then it will " +
        "teleport to the right side."
      )
    ] UnityOption<float> _maxDistanceToLeftSide;
    // FIXME: There's still a visual issue when all items fits into selection window
    // for example when selection window width is 500 and you have 3 elements of width 100. 
    [
      SerializeField, 
      InfoBox(
        "Width of a window for selection from the center of a carousel.\n" +
        "\n" +
        "When a new element is selected, its center will be moved so that it is within the " +
        "selection window. For example, if the window width is 0 then the selected element will " +
        "always be centered. If it will be 100, the selected element center point will always be " +
        "between x [-50; 50]."
      )
    ] float selectionWindowWidth;
    [
      SerializeField, NotNull,
      InfoBox(
        "Resizes set RectTransform depending on the size of the content." +
        "size = (distance between first and last items) + SpaceBetweenOtherPages"
      )
    ]
    UnityOptionRectTransform _maybeResizableTarget;


    /// <see cref="UIBehaviour.OnValidate"/> disappears in non-editor UnityEngine.dll.
    /// Therefore we have our own implementation here. Amazing.
    public new void OnValidate() {
      selectionWindowWidth = Math.Max(selectionWindowWidth, 0);
    }
#pragma warning restore 649

    #endregion

    readonly List<A> elements = new List<A>();

    /// <summary>
    /// Updates visual if we mutate elements.
    /// Done this way because it's more performant than immutable version
    /// </summary>
    public void editElements(Action<List<A>> f, bool animate = false) {
      f(elements);
      var pageValue = Mathf.Clamp(_page.value, 0, elements.Count - 1);
      if (animate)
        setPageAnimated(pageValue);
      else
        setPageInstantly(pageValue);
      updateCurrentElement();
    }

    public int elementsCount => elements.Count;
    public int indexOf(A a) => elements.IndexOf(a);

    // disables elements for which position from center exceeds this value
    /*[ReadOnly] */public Option<float> disableDistantElements = F.none<float>();
    bool loopable => wrapCarouselAround && elements.Count >= _minElementsForWraparound;

    readonly IRxRef<int> _page = RxRef.a(0);
    public IRxVal<int> page => _page;

    readonly LazyVal<IRxRef<Option<A>>> __currentElement;
    public IRxVal<Option<A>> currentElement => __currentElement.strict;

    [PublicAPI] public bool freezeCarouselMovement;

    void updateCurrentElement() {
      if (__currentElement.isCompleted) {
        __currentElement.strict.value = elements.get(_page.value);
      }
    }

    /// Page position between previously selected page index and <see cref="targetPageValue"/>
    Carousel.Pages currentPosition;

    /// A <see cref="Carousel"/> has two center points.
    /// * real one, determined by the game objects position.
    /// * logical one, which determines around which point all the items should be positioned.
    ///
    /// This specifies an offset, that is measured in pages, from the real center point, where
    /// the logical center point is. 
    ///
    /// If <see cref="selectionWindowWidth"/> is 0, this is always 0, as logical carousel center is always
    /// in the same point as real carousel center point.
    ///
    /// Otherwise this might drift so that the logical point ends up within half of
    /// <see cref="selectionWindowWidth"/>.
    /// 
    /// One page width is <see cref="SpaceBetweenSelectedAndAdjacentPages"/>
    Carousel.Pages centerPointOffset;

    int targetPageValue;
    public bool isMoving { get; private set; }
    readonly Subject<Unit> _movementComplete = new Subject<Unit>();
    public IRxObservable<Unit> movementComplete => _movementComplete;

    public void nextPage() => movePagesByAnimated(1);
    public void prevPage() => movePagesByAnimated(-1);

    protected Carousel() {
      __currentElement = Lazy.a(() => {
        var res = RxRef.a(elements.get(page.value));
        _page.subscribe(gameObject, p => res.value = elements.get(p));
        return res;
      });
    }

    /// <summary>Set page without any animations.</summary>
    public void setPageInstantly(int index) {
      currentPosition = Carousel.Pages.a(index);
      targetPageValue = index;
      _page.value = index;
    }

    /// <summary>Set page with smooth animations.</summary>
    public void setPageAnimated(int targetPage) {
      if (elements.isEmpty()) return;

      var currentOffset = targetPage - _page.value;

      if (wrapCarouselAround) {
        findBestPage(targetPage - _page.value - elementsCount);
        findBestPage(targetPage - _page.value + elementsCount);

        // Searches for shortest travel distance towards targetPage
        void findBestPage(int offset) {
          if (Math.Abs(centerPointOffset + offset) <= Math.Abs(currentOffset + centerPointOffset)) {
            currentOffset = offset;
          }
        }
      }

      movePagesByAnimated(currentOffset);
      isMoving = true;
    }

    void movePagesByAnimated(int offset) {
      if (elements.isEmpty()) return;

      if (loopable) {
        targetPageValue += offset;
        _page.value = targetPageValue.modPositive(elements.Count);
      }
      else {
        // when we increase past last page go to page 0 if wrapCarouselAround == true
        var page = offset + targetPageValue;
        targetPageValue = 
          wrapCarouselAround
          ? page.modPositive(elements.Count)
          : Mathf.Clamp(page, 0, elements.Count - 1);
        _page.value = targetPageValue;
      }
      isMoving = true;
    }

    public void Update() {
      lerpPosition(Time.deltaTime * 5);
    }

    UnityMeters toUnityMeters(Carousel.Pages pages) => 
      UnityMeters.a(pages.pages * SpaceBetweenSelectedAndAdjacentPages);
    
    void lerpPosition(float amount) {
      if (elements.isEmpty()) return;

      var withinMoveCompletedThreshold =
        Math.Abs(currentPosition - targetPageValue) < moveCompletedEventThreshold;

      if (isMoving && withinMoveCompletedThreshold) {
        isMoving = false;
        _movementComplete.push(F.unit);
      }

      var prevPos = currentPosition;
      currentPosition = Carousel.Pages.a(Mathf.Lerp(currentPosition, targetPageValue, amount));
      var positionDelta = currentPosition - prevPos;

      // Position is kept between 0 and elementsCount to
      // prevent scrolling multiple times if targetPageValue is something like 100 but we only have 5 elements
      {
        while (currentPosition > elementsCount) {
          currentPosition -= elementsCount;
          targetPageValue -= elementsCount;
        }

        while (currentPosition < 0) {
          currentPosition += elementsCount;
          targetPageValue += elementsCount;
        }
      }

      var itemCountFittingToWindow = selectionWindowWidth / SpaceBetweenOtherPages;
      centerPointOffset = Carousel.Pages.a(Mathf.Clamp(
        value: centerPointOffset + positionDelta, 
        min: -itemCountFittingToWindow / 2, 
        max: itemCountFittingToWindow / 2
      ));
      var centerPointInPages = 
        freezeCarouselMovement 
        ? currentPosition - (elementsCount - 1) / 2f 
        : centerPointOffset;

      float calculateAbsolutePositionDelta(int idx, float elementPos) => 
        Mathf.Abs(idx - elementPos + centerPointOffset);

      var maybeResizableRectData = Option<ResizableRectData>.None;
      for (var idx = 0; idx < elements.Count; idx++) {
        var elementPos = currentPosition;

        // Calculate element's position closest to pivot
        if (loopable) {
          var best = calculateAbsolutePositionDelta(idx, elementPos);
          void findBestElementPosition(Carousel.Pages newElementPosition) {
            var current = calculateAbsolutePositionDelta(idx, newElementPosition);
            if (current < best) {
              best = current;
              elementPos = newElementPosition;
            }
          }

          findBestElementPosition(currentPosition - elements.Count);
          findBestElementPosition(currentPosition + elements.Count);
        }

        var baseDiff = idx - elementPos;
        
        var diffWithSign = _maxDistanceToLeftSide.valueOut(out var maxDistanceToLeft)
          ? (baseDiff < -maxDistanceToLeft ? baseDiff + elements.Count : baseDiff)
          : baseDiff;

        var absoluteDiff = Math.Abs(diffWithSign);
        
        var deltaPos = UnityMeters.a(
          Mathf.Clamp01(absoluteDiff) * SpaceBetweenSelectedAndAdjacentPages 
          + Mathf.Max(0, absoluteDiff - 1) * SpaceBetweenOtherPages
        );

        foreach (var distance in disableDistantElements) {
          elements[idx].gameObject.SetActive(deltaPos < distance);
        }

        var go = elements[idx].gameObject;
        var t = go.transform;

        var sign = Mathf.Sign(diffWithSign);
        t.localPosition = getPosition(
          carouselDirection: _direction,
          elementDistanceFromCenter: deltaPos * sign,
          absoluteDifference: absoluteDiff,
          centralItemOffset: selectedPageOffset,
          centerPoint: toUnityMeters(centerPointInPages)
        );

        t.localScale = Vector3.one * (
          absoluteDiff < 1
          ? Mathf.Lerp(SelectedPageItemsScale, OtherPagesItemsScale, absoluteDiff)
          : (
            maxElementsFromCenter.value.isNone
            ? Mathf.Lerp(OtherPagesItemsScale, AdjacentToSelectedPageItemsScale, absoluteDiff - 1)
            : Mathf.Lerp(
              AdjacentToSelectedPageItemsScale, 0, absoluteDiff - maxElementsFromCenter.value.__unsafeGet
            )
          )
        );

        if (go.activeSelf && _maybeResizableTarget.valueOut(out var target)) {
          if (maybeResizableRectData.valueOut(out var resizableRectData)) {
            var fvTr = resizableRectData.firstVisibleItem;
            var lvTr = resizableRectData.lastVisibleItem;
            if (
              _direction == Carousel.Direction.Horizontal
                ? fvTr.localPosition.x > t.localPosition.x
                : fvTr.localPosition.y > t.localPosition.y
            ) {
              maybeResizableRectData = Some.a(resizableRectData.withFirstVisibleItem(t));
            }
            else if (
              _direction == Carousel.Direction.Horizontal
                ? lvTr.localPosition.x < t.localPosition.x
                : lvTr.localPosition.y < t.localPosition.y
            ) {
              maybeResizableRectData = Some.a(resizableRectData.withLastVisibleItem(t));
            }
          }
          else {
            maybeResizableRectData = Some.a(new ResizableRectData(t, t, target));
          }
        }
      }

      {if (maybeResizableRectData.valueOut(out var resizableData)) {
        var resizableTarget = resizableData.target;
        var size =
          getPos(resizableData.lastVisibleItem) - getPos(resizableData.firstVisibleItem) + SpaceBetweenOtherPages;


        resizableTarget.SetSizeWithCurrentAnchors(
          _direction == Carousel.Direction.Horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical,
          size
        );

        float getPos(Transform tr) =>
          _direction == Carousel.Direction.Horizontal ? tr.localPosition.x : tr.localPosition.y;
      }}
    }

    static Vector3 getPosition(
      Carousel.Direction carouselDirection, UnityMeters elementDistanceFromCenter, 
      float absoluteDifference, Option<Vector3> centralItemOffset, UnityMeters centerPoint
    ) {
      var newPosition = 
        carouselDirection == Carousel.Direction.Horizontal
        ? new Vector3(elementDistanceFromCenter + centerPoint, 0, 0)
        : new Vector3(0, -elementDistanceFromCenter - centerPoint, 0);

      if (centralItemOffset.valueOut(out var offset)) {
        var lerpedOffset = Vector3.Lerp(offset, Vector3.zero, absoluteDifference);
        return newPosition + lerpedOffset;
      }

      return newPosition;
    }

    /// <summary>
    /// Immediately refresh carousel content. Call after modifying <see cref="elements"/> to prevent
    /// visual flicker.
    /// </summary>
    public void forceUpdate() => lerpPosition(1);

    public void handleCarouselSwipe(SwipeDirection swipeDirection) {
      switch (_direction) {
        case Carousel.Direction.Horizontal:
          switch (swipeDirection) {
            case SwipeDirection.Left:
              nextPage();
              break;
            case SwipeDirection.Right:
              prevPage();
              break;
            case SwipeDirection.Up:
            case SwipeDirection.Down:
              break;
            default:
              throw new ArgumentOutOfRangeException(nameof(swipeDirection), swipeDirection, null);
          }
          break;
        case Carousel.Direction.Vertical:
          switch (swipeDirection) {
            case SwipeDirection.Left:
            case SwipeDirection.Right:
              break;
            case SwipeDirection.Up:
              nextPage();
              break;
            case SwipeDirection.Down:
              prevPage();
              break;
            default:
              throw new ArgumentOutOfRangeException(nameof(swipeDirection), swipeDirection, null);
          }
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(_direction), _direction, null);
      }
    }

    public void OnDrawGizmosSelected() {
      Gizmos.color = Color.blue;
      var rectTransform = (RectTransform) transform;
      var lineLength =
        _direction == Carousel.Direction.Horizontal
          ? rectTransform.rect.height
          : rectTransform.rect.width;
      var halfOfSelectionWindow = selectionWindowWidth / 2;
      var halfLineLength = lineLength / 2;

      Vector3 t(Vector3 v) => transform.TransformPoint(v);

      switch (_direction) {
        case Carousel.Direction.Horizontal:
          Gizmos.DrawLine(
            t(new Vector3(-halfOfSelectionWindow, -halfLineLength)), 
            t(new Vector3(-halfOfSelectionWindow, halfLineLength))
          );
          Gizmos.DrawLine(
            t(new Vector3(halfOfSelectionWindow, -halfLineLength)), 
            t(new Vector3(halfOfSelectionWindow, halfLineLength))
          );
          break;
        case Carousel.Direction.Vertical:
          Gizmos.DrawLine(
            t(new Vector3(-halfLineLength, -halfOfSelectionWindow)), 
            t(new Vector3(halfLineLength, -halfOfSelectionWindow))
          );
          Gizmos.DrawLine(
            t(new Vector3(-halfLineLength, halfOfSelectionWindow)), 
            t(new Vector3(halfLineLength, halfOfSelectionWindow))
          );
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(_direction), _direction, null);
      }
    }
  }

  public struct CarouselGameObject : ICarouselItem {
    public GameObject gameObject { get; }

    public CarouselGameObject(GameObject gameObject) {
      this.gameObject = gameObject;
    }

    public static implicit operator CarouselGameObject(GameObject o) => new CarouselGameObject(o);
  }
}