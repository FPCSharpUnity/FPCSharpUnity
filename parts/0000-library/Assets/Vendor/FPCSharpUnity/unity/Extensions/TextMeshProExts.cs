using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.reactive;
using GenerationAttributes;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static partial class TMP_TextMeshProExts {
    /// <summary>Returns an event stream of <see cref="TMP_InputField.text"/> changes.</summary>
    public static IRxObservable<string> textChanges(this TMP_InputField inputField) =>
      Observable.fromEvent2<string, UnityAction<string>>(
        registerCallback: push => {
          var callback = new UnityAction<string>(push);
          inputField.onValueChanged.AddListener(callback);
          return callback;
        },
        unregisterCallback: callback => inputField.onValueChanged.RemoveListener(callback)
      );

    /// <summary>Returns a reactive version of <see cref="TMP_InputField.text"/>.</summary>
    public static IRxVal<string> textRx(this TMP_InputField inputField, IRxObservableToIRxValMode mode) =>
      inputField.textChanges().toRxVal(mode, inputField.text);

    /// <summary>Searches for an intersection with a <see cref="TMP_LinkInfo"/>.</summary>
    /// <param name="text">Text to look the intersection for.</param>
    /// <param name="pos">Position in screen space.</param>
    /// <param name="camera">Camera used to transform the provided <see cref="pos"/>
    /// into world space with text's transform taken into account.</param>
    /// <returns>Some(<see cref="IntersectingLinkInfo"/>) if an intersection has been found with a link info.</returns>
    /// <remarks>
    /// Identical to <see cref="TMP_TextUtilities.FindIntersectingLink"/>, but returns additional information.
    /// </remarks>
    public static Option<IntersectingLinkInfo> intersectWithLink(
      this TMP_Text text, Vector2 pos, Camera camera
    ) {
      var position = pos.toVector3XY();
      var rectTransform = text.transform;

      // Convert position into Worldspace coordinates
      TMP_TextUtilities.ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

      for (var i = 0; i < text.textInfo.linkCount; i++) {
        var linkInfo = text.textInfo.linkInfo[i];

        var isBeginRegion = false;

        var bl = Vector3.zero;
        var tl = Vector3.zero;
        var tr = Vector3.zero;

        // Iterate through each character of the word
        for (var j = 0; j < linkInfo.linkTextLength; j++) {
          var characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
          var currentCharInfo = text.textInfo.characterInfo[characterIndex];
          var currentLine = currentCharInfo.lineNumber;

          // Check if Link characters are on the current page
          if (text.overflowMode == TextOverflowModes.Page && currentCharInfo.pageNumber + 1 != text.pageToDisplay) continue;

          if (isBeginRegion == false) {
            isBeginRegion = true;

            var obl = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
            var otl = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

            bl = rectTransform.TransformPoint(obl);
            tl = rectTransform.TransformPoint(otl);

            // If Word is one character
            if (linkInfo.linkTextLength == 1) {
              isBeginRegion = false;

              var otr = new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

              tr = rectTransform.TransformPoint(otr);

              if (intersects().bindTo(out var ili)) return ili;
            }
          }

          // Last Character of Word
          if (isBeginRegion && j == linkInfo.linkTextLength - 1) {
            isBeginRegion = false;

            var otr = new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

            tr = rectTransform.TransformPoint(otr);

            if (intersects().bindTo(out var ili)) return ili;
          }
          // If Word is split on more than one line.
          else if (isBeginRegion && currentLine != text.textInfo.characterInfo[characterIndex + 1].lineNumber) {
            isBeginRegion = false;

            var otr = new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

            tr = rectTransform.TransformPoint(otr);

            if (intersects().bindTo(out var ili)) return ili;
          }
        }

        // Checks for intersection using current state and returns Some, if intersection has been detected.
        Option<IntersectingLinkInfo> intersects() {
          var ab = tl - bl;
          var am = position - bl;
          var bc = tr - tl;
          var bm = position - tl;

          var abamDot = Vector3.Dot(ab, am);
          var bcbmDot = Vector3.Dot(bc, bm);

          if (!(
                0 <= abamDot
                && 0 <= bcbmDot
                && abamDot <= Vector3.Dot(ab, ab)
                && bcbmDot <= Vector3.Dot(bc, bc)
              )) return None._;
          else return
            (new IntersectingLinkInfo(
              linkInfo, new Rect(bl, tr - bl)
            )).some();
        }
      }

      return None._;
    }

    /// <summary>
    /// Contains information about a <see cref="TMP_LinkInfo"/> that's been found
    /// during the intersection test <see cref="TMP_TextMeshProExts.intersectWithLink"/>.
    /// </summary>
    [Record] public readonly partial struct IntersectingLinkInfo {
      /// <summary>The intersected link's index within the <see cref="TMP_Text"/>.</summary>
      public readonly TMP_LinkInfo linkInfo;

      /// <summary>
      /// The intersected link's boundaries in the text's local space.
      /// <remarks>Use text's <see cref="Transform"/> to transform into world space.</remarks>
      /// </summary>
      public readonly Rect localBounds;
    }
  }
}