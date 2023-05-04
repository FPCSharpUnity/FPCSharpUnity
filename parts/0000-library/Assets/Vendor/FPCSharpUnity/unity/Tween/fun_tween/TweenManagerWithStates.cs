using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.pools;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Tween.fun_tween.serialization.manager;
using FPCSharpUnity.unity.unity_serialization;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween; 

/// <summary>
/// Useful tool for commonly used pattern:<br/>
/// Split tween manager's timeline into multiple timestamps, each time slot representing a state.<br/>
/// This controller script will help switching between these states seamlessly by lerping tween time between states'
/// timestamps slots.
/// </summary>
/// <example>
/// Lets have enum states: Closed, Preview, Opened.<br/>
/// Timestamps percentages for these could be respectively: 0%, 50%, 100%.<br/>
/// If we want to transition between Closed and Preview states, we lerp time from 0% to 50% of total tween time.<br/>
/// </example>
/// <typeparam name="State">Enum for states.</typeparam>
[Serializable]
public class TweenManagerWithStates<State, EnumTypeAType>
  where State : unmanaged, Enum
  where EnumTypeAType : IEnumTypeAType<State, Percentage>
{
#pragma warning disable 649
  // ReSharper disable NotNullMemberIsNotInitialized
  [SerializeField, NotNull] FunTweenManagerV2 _tween;
  [
    SerializeField, NotNull, ValidateInput(nameof(validateTimestamps))
  ] EnumTypeAType _timestamps;
  // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

  IDisposable currentAnimation = F.emptyDisposable;

  // ReSharper disable once InconsistentNaming
  [ShowInInspector, ReadOnly] State _editor_currentStatePreview;

#if UNITY_EDITOR
  [OnInspectorGUI] void OnInspectorGUI() {
    if (!_tween) return;

    var setDefaultValues =
      EnumUtils<State>.valuesArray.All(e => _timestamps[e] == default(Percentage));

    for (var i = 0; i < EnumUtils<State>.valuesArray.Count; i++) {
      var state = EnumUtils<State>.valuesArray[i];
      if (setDefaultValues) _timestamps._editor_set(
        state, new Percentage(i / (float)(EnumUtils<State>.valuesArray.Count - 1))
      );
      if (GUILayout.Button($"Apply `{state}`")) {
        _tween.timeline.timePassed = timeAtState(state);
        _editor_currentStatePreview = state;
      }
    }
  }
#endif

  void runAnimation(float target) {
    currentAnimation.Dispose();
    currentAnimation = new UnityCoroutine(_tween, run());

    IEnumerator run() {
      while (Mathf.Abs(_tween.timeline.timePassed - target) > 1e-3) {
        _tween.timeline.timePassed = Mathf.MoveTowards(_tween.timeline.timePassed, target, Time.deltaTime);
        yield return null;        
      }
    }
  }

  float timeAtState(State state) => _tween.timeline.duration * _timestamps[state].value;

  /// <summary> Stop running animation and instantly set state to given value. </summary>
  public void resetTo(State state) {
    currentAnimation.Dispose();
    _tween.timeline.timePassed = timeAtState(state);
    _editor_currentStatePreview = state;
  }

  /// <summary> Stop running animation and start new one, transitioning to given state. </summary>
  public void transitionTo(State state) {
    // Can't start coroutines if the game object is disabled - so set the state instantly.
    if (_tween.gameObject.activeInHierarchy) {
      runAnimation(timeAtState(state));
    }
    else {
      resetTo(state);
    }
    _editor_currentStatePreview = state;     
  }

  bool validateTimestamps(EnumTypeAType _, ref string msg) {
    if (!_tween) return false;
    using var listDisposable = ListPool<ErrorMsg>.instance.BorrowDisposable();
    var list = listDisposable.value;

    foreach (var kvp in EnumUtils<State>.valuesArray.Select(e => (value: _timestamps[e], e)).GroupBy(_ => _.value)) {
      if (kvp.Count() > 1)
        list.Add(new ErrorMsg($"Duplicate timestamp {kvp.Key} for states {kvp.Select(_ => _.e).mkString(", ")}"));
    }

    msg = list.mkString("\n");
    return list.isEmpty();
  }
}