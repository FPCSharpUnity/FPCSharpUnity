using System.Collections.Generic;
using System.Linq;
using ExhaustiveMatching;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Data;
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
    [SerializeField, NotNull] RectTransform _rt;
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
    bool forceRefresh;

#pragma warning disable 649
    [ShowInInspector] float __editor_leftOffsetTest, __editor_rightOffsetTest, __editor_bottomOffsetTest;
#pragma warning restore 649

    public static bool validateNonZeroOffsets(List<RectTransform> rts, ref string errors) {
      foreach (var rt in rts) {
        if ((rt.offsetMin != Vector2.zero || rt.offsetMax != Vector2.zero) && !Application.isPlaying) {
          errors += $"RectTransform {rt.name} should have zero offsets, because it will be overwritten by "
                    + $"{nameof(ResizeToSafeAreaOffsets)} and the UI will no longer match the design.";
        }
      }
      return errors.isNullOrEmpty();
    }

    protected override void Awake() {
      parent = (RectTransform) _rt.parent;
      forceRefresh = true;
      refresh();
    }

    public void Update() => refresh();

    public void addToNegativeOffsetLeft(RectTransform t) {
      _negativeOffsetLeft.Add(t);
      forceRefresh = true;
    }

    public void addToNegativeOffsetRight(RectTransform t) {
      _negativeOffsetRight.Add(t);
      forceRefresh = true;
    }

    public void addToNegativeOffsetAll(RectTransform t) {
      _negativeOffsetAll.Add(t);
      forceRefresh = true;
    }

    public void addToNegativeOffsetBottom(RectTransform t) {
      _negativeOffsetBottom.Add(t);
      forceRefresh = true;
    }

    public void addToNegativeOffsetSidesWithoutNotches(RectTransform t) {
      _negativeOffsetSidesWithoutNotches.Add(t);
      forceRefresh = true;
    }

    public void addToNegativeOffsetLeftRightWithoutNotches(RectTransform t) {
      _negativeOffsetLeftRightWithoutNotches.Add(t);
      forceRefresh = true;
    }

    void refresh() {
      var safeArea = Screen.safeArea;
      if (Application.isEditor) {
        safeArea.xMin += __editor_leftOffsetTest;
        safeArea.xMax -= __editor_rightOffsetTest;
        safeArea.yMin += __editor_bottomOffsetTest;
      }
      var orientation = Screen.orientation;
      
      if (forceRefresh || safeArea != lastSafeArea || orientation != lastScreenOrientation) {
        forceRefresh = false;
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
        
        log.mInfo(
          $"Result: {notchLeft.echo()}, {notchRight.echo()}, {orientation.echo()}, "
          + $"{safeArea.echo()}, Screen.width={width}, Screen.height={height}, "
          + $"cutouts={Screen.cutouts.Select(a => a.ToString()).mkString(", ")}, "
          + $"{useManualCutoutsDetection.echo()}, {offsetFromLeft.echo()}, {offsetFromRight.echo()}"
        );
        
        applySafeArea(
          safeArea: safeArea, screenSize: new Vector2(width, height), notchLeft: notchLeft, notchRight: notchRight
        );
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
        item.offsetMin = Vector2.zero;
        item.offsetMax = Vector2.zero;
        if (!notchLeft) negativeLeft(item, custom: custom);
        if (!notchRight) negativeRight(item, custom: custom);
        if (topAndBottom) {
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
        offset.x = (custom ? _customNegativeOffset.value : 1) * -min.x;
        item.offsetMin = offset;
      }
      
      void negativeRight(RectTransform item, bool custom = false) {
        var offset = item.offsetMax;
        offset.x = (custom ? _customNegativeOffset.value : 1) * max.x;
        item.offsetMax = offset;
      }
      
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

    protected override void Reset() {
      base.Reset();
      _rt = (RectTransform)transform;
    }
  }
}