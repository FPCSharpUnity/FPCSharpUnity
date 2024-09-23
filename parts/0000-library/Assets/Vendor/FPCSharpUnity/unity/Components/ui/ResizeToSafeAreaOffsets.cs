using System.Collections.Generic;
using System.Linq;
using ExhaustiveMatching;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components.ui {
  [
    HasLogger, TypeInfoBox(
      $"Adds/removes offsets to the edges of `{nameof(_rt)}` to match safe area. "
      + $"It also contains negativeOffsetsX lists, which "
      + $"are used to revert the offsets done by offsetting `{nameof(_rt)}` on specific side/other conditions "
      + $"(blacklist style).\n"
      + $"Example: typical iPhone has safe area offsets from left (lets say 50px), right (50px) and bottom (20px) screen "
      + $"edges. By adding a child to `{nameof(_negativeOffsetAll)}`, it will make the UI look like nothing was "
      + $"offset at all."
    )
  ] public partial class ResizeToSafeAreaOffsets : UIBehaviour, IMB_Update {
#pragma warning disable 649
// ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
    [SerializeField, NotNull, ValidateInput(nameof(validateAllForOdinAttribute))] RectTransform _rt;
    [SerializeField, NotNull, ValidateInput(nameof(validateNonZeroOffsets)), InfoBox(
      $"Remove offsets from the left side of the screen for these child {nameof(RectTransform)}s."
    )] List<RectTransform> _negativeOffsetLeft;

    [SerializeField, NotNull, ValidateInput(nameof(validateNonZeroOffsets)), InfoBox(
      $"Remove offsets from the right side of the screen for these child {nameof(RectTransform)}s."
    )] List<RectTransform> _negativeOffsetRight;

    [SerializeField, NotNull, ValidateInput(nameof(validateNonZeroOffsets)), InfoBox(
      $"Remove offsets from all sides (left, right, top, bottom) of the screen for these child {nameof(RectTransform)}s."
    )] List<RectTransform> _negativeOffsetAll;

    [SerializeField, NotNull, ValidateInput(nameof(validateNonZeroOffsets)), InfoBox(
      $"Remove offsets from the bottom of the screen for these child {nameof(RectTransform)}s."
    )] List<RectTransform> _negativeOffsetBottom;

    [SerializeField, NotNull, ValidateInput(nameof(validateNonZeroOffsets)), InfoBox(
      $"Remove offsets from all sides except the sides which contain a notch. Useful when safe area does not have a "
      + $"notch there, but the UI still get offset by it."
    )] List<RectTransform> _negativeOffsetSidesWithoutNotches;

    [SerializeField, NotNull, ValidateInput(nameof(validateNonZeroOffsets)), InfoBox(
       $"Like `{nameof(_negativeOffsetSidesWithoutNotches)}`, but using `{nameof(_customNegativeOffset)}`."
    )] List<RectTransform> _customNegativeOffsetOnSidesWithoutNotches;

    [SerializeField, NotNull, ValidateInput(nameof(validateNonZeroOffsets)), InfoBox(
       $"Remove offsets from left or right sides except the sides which contain a notch. Useful when safe area does not "
       + $"have a notch there, but the UI still get offset by it."
    )] List<RectTransform> _negativeOffsetLeftRightWithoutNotches;

    [SerializeField, NotNull] Percentage _customNegativeOffset = new Percentage(.5f);
// ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649

    RectTransform parent;
    [ShowInInspector, ReadOnly] Rect lastSafeArea = new Rect(0, 0, 0, 0);
    [ShowInInspector, ReadOnly] ScreenOrientation lastScreenOrientation;
    [ShowInInspector, ReadOnly] bool lastNotchOnLeft, lastNotchOnRight;
    
    /// <summary>
    /// If `Some` - layout will be updated on next <see cref="Update"/> call and this flag will be reset.
    /// </summary>
    Option<ForceRefreshType> maybeForceRefresh;

    public enum ForceRefreshType {
      /// <summary> Refresh was requested from <see cref="ResizeToSafeAreaOffsets.Awake"/> call. </summary>
      Initial, 
      
      /// <summary>
      /// Refresh was requested from <see cref="ResizeToSafeAreaOffsets"/>.addToNegativeOffsetToX call.
      /// </summary>
      AddedNegativeOffset
    }

#pragma warning disable 649
    [ShowInInspector] float __editor_leftOffsetTest, __editor_rightOffsetTest, __editor_bottomOffsetTest;
#pragma warning restore 649

    public static bool validateNonZeroOffsets(List<RectTransform> rts, ref string errors) {
      foreach (var rt in rts) {
        if ((rt.offsetMin != Vector2.zero || rt.offsetMax != Vector2.zero) && !Application.isPlaying) {
          errors += $"RectTransform `{rt.name}` should have zero offsets, because it will be overwritten by "
                    + $"{nameof(ResizeToSafeAreaOffsets)} and the UI will no longer match the design.";
        }
      }
      return errors.isNullOrEmpty();
    }

    protected override void Awake() {
      parent = (RectTransform) _rt.parent;
      maybeForceRefresh = Some.a(ForceRefreshType.Initial);
      refresh();
    }

    public void Update() => refresh();

    public void addToNegativeOffsetLeft(RectTransform t) {
      _negativeOffsetLeft.Add(t);
      maybeForceRefresh = Some.a(ForceRefreshType.AddedNegativeOffset);
    }

    public void addToNegativeOffsetRight(RectTransform t) {
      _negativeOffsetRight.Add(t);
      maybeForceRefresh = Some.a(ForceRefreshType.AddedNegativeOffset);
    }

    public void addToNegativeOffsetAll(RectTransform t) {
      _negativeOffsetAll.Add(t);
      maybeForceRefresh = Some.a(ForceRefreshType.AddedNegativeOffset);
    }

    public void addToNegativeOffsetBottom(RectTransform t) {
      _negativeOffsetBottom.Add(t);
      maybeForceRefresh = Some.a(ForceRefreshType.AddedNegativeOffset);
    }

    public void addToNegativeOffsetSidesWithoutNotches(RectTransform t) {
      _negativeOffsetSidesWithoutNotches.Add(t);
      maybeForceRefresh = Some.a(ForceRefreshType.AddedNegativeOffset);
    }

    public void addToNegativeOffsetLeftRightWithoutNotches(RectTransform t) {
      _negativeOffsetLeftRightWithoutNotches.Add(t);
      maybeForceRefresh = Some.a(ForceRefreshType.AddedNegativeOffset);
    }

    void refresh() {
      var safeArea = Screen.safeArea;
      if (Application.isEditor) {
        safeArea.xMin += __editor_leftOffsetTest;
        safeArea.xMax -= __editor_rightOffsetTest;
        safeArea.yMin += __editor_bottomOffsetTest;
      }
      var orientation = Screen.orientation;
      var addedNegativeOffset = maybeForceRefresh.foldM(false, t => t is ForceRefreshType.AddedNegativeOffset);
      
      if (maybeForceRefresh.isSome || safeArea != lastSafeArea || orientation != lastScreenOrientation) {
        maybeForceRefresh = None._;
        lastSafeArea = safeArea;
        lastScreenOrientation = orientation;

        var notchLeft = false;
        var notchRight = false;
        var width = Screen.width;
        var height = Screen.height;
        var cutouts = Screen.cutouts;
        bool useManualCutoutsDetection;
        var offsetFromLeft = safeArea.xMin;
        var offsetFromRight = width - safeArea.xMax;
        
        {
          // this works only for landscape
          var halfX = width / 2;
          foreach (var cutout in cutouts) {
            if (cutout.center.x < halfX) notchLeft = true;
            else notchRight = true;
          }
          
          // Problem: we can't trust `Screen.cutouts`, as on most of the iPhones it returns an empty array.
          useManualCutoutsDetection = cutouts.isEmpty() && !notchLeft && !notchRight;
          
          if (useManualCutoutsDetection) {
            // Simple solution - assume that the notch is always on the top (when looking at the phone in portrait
            // orientation).
            assumeNotchIsAlwaysOnTheTop();
            
            void assumeNotchIsAlwaysOnTheTop() {
              switch (orientation) {
                case ScreenOrientation.LandscapeLeft:
                  notchLeft = offsetFromLeft > 0;
                  break;
                case ScreenOrientation.LandscapeRight:
                  notchRight = offsetFromRight > 0;
                  break;
                case ScreenOrientation.PortraitUpsideDown:
                case ScreenOrientation.AutoRotation:
                case ScreenOrientation.Portrait:
#pragma warning disable CS0618
                case ScreenOrientation.Unknown:
#pragma warning restore CS0618
                  break;
                default: throw ExhaustiveMatch.Failed(orientation);
              }
            }
          }
        }
        
        log.mDebug(
          $"Result: {notchLeft.echo()}, {notchRight.echo()}, {orientation.echo()}, "
          + $"{safeArea.echo()}, Screen.width={width}, Screen.height={height}, "
          + $"cutouts={Screen.cutouts.Select(a => a.ToString()).mkString(", ")}, "
          + $"{useManualCutoutsDetection.echo()}, {offsetFromLeft.echo()}, {offsetFromRight.echo()}"
        );
        
        applySafeArea(
          safeArea: safeArea, screenSize: new Vector2(width, height), notchLeft: notchLeft, notchRight: notchRight
        );
      }
      
      // This will allow us to catch runtime UI offset bugs, which can't be validated at build time.
      if (addedNegativeOffset) {
        var level = Application.isEditor ? LogLevel.ERROR : LogLevel.INFO;
        if (log.willLog(level)) {
          var errors = validateAllThis().toImmutableArrayC();
          if (!errors.isEmpty()) {
            log.log(level, $"Runtime check for illegal offsets found potential bugs: {errors.mkStringEnumNewLines()}", this);
          }
        }
      }
    }

    void applySafeArea(Rect safeArea, Vector2 screenSize, bool notchLeft, bool notchRight) {
      var scale = parent.rect.size / screenSize;

      lastNotchOnLeft = notchLeft;
      lastNotchOnRight = notchRight;

      var min = safeArea.min * scale;
      var max = (screenSize - safeArea.max) * scale;

      _rt.offsetMin = min;
      // offsetMax is inverted in unity
      _rt.offsetMax = -max;
      
      foreach (var item in _negativeOffsetLeftRightWithoutNotches) {
        negativeOffsetWithoutNotches(item, custom: false, topAndBottom: false);
      }
      
      foreach (var item in _negativeOffsetSidesWithoutNotches) {
        negativeOffsetWithoutNotches(item, custom: false, topAndBottom: true);
      }
      
      foreach (var item in _customNegativeOffsetOnSidesWithoutNotches) {
        negativeOffsetWithoutNotches(item, custom: true, topAndBottom: true);
      }

      void negativeOffsetWithoutNotches(RectTransform item, bool custom, bool topAndBottom) {
        // reset old values, otherwise all sides will get negative offsets after we rotate the screen 
        var offsetMin = Vector2.zero;
        var offsetMax = Vector2.zero;
        
        if (!notchLeft) offsetMin.x = negativeLeftValue(custom);
        if (!notchRight) offsetMax.x = negativeRightValue(custom);
        
        // Setting offset values multiple times per frame causes glitches in `DynamicLayout`
        item.offsetMin = offsetMin;
        item.offsetMax = offsetMax;
        
        if (topAndBottom) {
          // TODO: implement these via local variables.
          negativeTop(item);
          negativeBottom(item);          
        }
      }

      foreach (var item in _negativeOffsetLeft) {
        negativeLeft(item);
      }

      foreach (var item in _negativeOffsetRight) {
        negativeRight(item);
      }

      foreach (var item in _negativeOffsetAll) {
        item.offsetMin = -min;
        item.offsetMax = max;
      }

      foreach (var item in _negativeOffsetBottom) {
        var offset = item.offsetMin;
        offset.y = -min.y;
        item.offsetMin = offset;
      }

      void negativeLeft(RectTransform item, bool custom = false) {
        var offset = item.offsetMin;
        offset.x = negativeLeftValue(custom);
        item.offsetMin = offset;
      }
      
      float negativeLeftValue(bool custom) => (custom ? _customNegativeOffset.value : 1) * -min.x;
      
      void negativeRight(RectTransform item, bool custom = false) {
        var offset = item.offsetMax;
        offset.x = negativeRightValue(custom);
        item.offsetMax = offset;
      }
      
      float negativeRightValue(bool custom) => (custom ? _customNegativeOffset.value : 1) * max.x;
      
      void negativeTop(RectTransform item) {
        var offset = item.offsetMin;
        offset.y = -min.y;
        item.offsetMin = offset;
      }
      
      void negativeBottom(RectTransform item) {
        var offset = item.offsetMax;
        offset.y = max.y;
        item.offsetMax = offset;
      }
    }

    bool validateAllForOdinAttribute(RectTransform _, ref string errors) {
      var errorMsg = validateAllThis().toImmutableArrayC();
      if (errorMsg.isEmpty()) return true;
      
      errors = errorMsg.Select(e => e.s).mkStringEnumNewLines();
      return false;
    }

    IEnumerable<ErrorMsg> validateAllThis() => validateAll(
      gameObject,
      negativeOffsetLeft: _negativeOffsetLeft,
      negativeOffsetRight: _negativeOffsetRight,
      negativeOffsetAll: _negativeOffsetAll,
      negativeOffsetBottom: _negativeOffsetBottom,
      negativeOffsetSidesWithoutNotches: _negativeOffsetSidesWithoutNotches,
      customNegativeOffsetOnSidesWithoutNotches: _customNegativeOffsetOnSidesWithoutNotches,
      negativeOffsetLeftRightWithoutNotches: _negativeOffsetLeftRightWithoutNotches
    );

    public static IEnumerable<ErrorMsg> validateAll(
      GameObject gameObjectWhereTheComponentIsAttached,
      IReadOnlyList<RectTransform> negativeOffsetLeft,
      IReadOnlyList<RectTransform> negativeOffsetRight,
      IReadOnlyList<RectTransform> negativeOffsetAll,
      IReadOnlyList<RectTransform> negativeOffsetBottom,
      IReadOnlyList<RectTransform> negativeOffsetSidesWithoutNotches,
      IReadOnlyList<RectTransform> customNegativeOffsetOnSidesWithoutNotches,
      IReadOnlyList<RectTransform> negativeOffsetLeftRightWithoutNotches
    ) {
      var offsetsLeft = negativeOffsetLeft
        .Concat(negativeOffsetAll)
        .Concat(negativeOffsetSidesWithoutNotches)
        .Concat(negativeOffsetLeftRightWithoutNotches)
        .Concat(customNegativeOffsetOnSidesWithoutNotches)
        .toImmutableArrayC();
      var offsetsRight = negativeOffsetRight
        .Concat(negativeOffsetAll)
        .Concat(negativeOffsetSidesWithoutNotches)
        .Concat(negativeOffsetLeftRightWithoutNotches)
        .Concat(customNegativeOffsetOnSidesWithoutNotches)
        .toImmutableArrayC();
      var offsetsBottom = negativeOffsetBottom
        .Concat(negativeOffsetAll)
        .Concat(negativeOffsetSidesWithoutNotches)
        .Concat(customNegativeOffsetOnSidesWithoutNotches)
        .toImmutableArrayC();
      
      return checkSide(offsetsLeft, "left")
        .Concat(checkSide(offsetsRight, "right"))
        .Concat(checkSide(offsetsBottom, "bottom"))
        .Concat(checkForNestedResizeToSafeAreaOffsets());

      IEnumerable<ErrorMsg> checkForNestedResizeToSafeAreaOffsets() =>
        gameObjectWhereTheComponentIsAttached
          .getComponentInParents<ResizeToSafeAreaOffsets>(includeSelf: false).mapM(r => new ErrorMsg(
            $"Nested `<b>{nameof(ResizeToSafeAreaOffsets)}</b>` components are not supported! {r.transform.debugPath()}"  
          )).asEnumerable();

      IEnumerable<ErrorMsg> checkSide(ImmutableArrayC<RectTransform> rts, string sideName) {
        foreach (var duplicateRt in rts.GroupBy(_ => _).Where(_ => _.Count() > 1)) {
          yield return new ErrorMsg(
            $"`<b>{duplicateRt.Key.debugPath()}</b>` has multiple negative offsets to the {sideName} side defined! "
            + $"Only one can work at a time!"  
          );
        }
        foreach (var rt1 in rts) {
          foreach (var rt2 in rts) {
            if (rt1 != rt2 && isParentOf(rt1, rt2)) {
              yield return new ErrorMsg(
                $"`<b>{rt1.debugPath()}</b>` is a child of `<b>{rt2.debugPath()}</b>` and has a negative offset to the "
                + $"{sideName} side defined! Doing offset multiple times will result in broken UI!"  
              );
            }
          }
        }
        bool isParentOf(Transform child, Transform maybeParent) {
          var p = child.parent;
          while (p != null) {
            if (p == maybeParent) return true;
            p = p.parent;
          }
          return false;
        }
      }      
    }

#if UNITY_EDITOR
    protected override void Reset() {
      base.Reset();
      _rt = (RectTransform)transform;
    }
#endif
  }
}