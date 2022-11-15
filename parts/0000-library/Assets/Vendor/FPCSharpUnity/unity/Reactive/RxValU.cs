using System;
using System.Collections;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.unity.Concurrent;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.reactive;
using UnityEngine;

namespace FPCSharpUnity.unity.Reactive {
  [PublicAPI] public static class RxValU {
    
    /// <summary>
    /// Calls <see cref="func"/> on update until it returns Some and stores the result in <see cref="RxVal"/>.
    /// </summary>
    public static IRxVal<Option<A>> fromBusyLoop<A>(Func<Option<A>> func, YieldInstruction delay=null) {
      var rx = RxRef.a(Option<A>.None);
      ASync.StartCoroutine(coroutine());
      return rx;

      IEnumerator coroutine() {
        while (true) {
          var maybeValue = func();
          if (maybeValue.valueOut(out var value)) {
            rx.value = Some.a(value);
            yield break;
          }
          else {
            yield return delay;
          }
        }
      }
    }

    /// <summary>
    /// Calls <see cref="getValue"/> every frame and updates the resulting <see cref="RxVal"/>.
    /// </summary>
    public static IRxVal<A> everyFrame<A>(ITracker tracker, Func<A> getValue) {
      var result = RxRef.a(getValue());
      ASync.onUpdate.subscribe(tracker, _ => result.value = getValue());
      return result;
    }

    /// <summary>
    /// Provides a <see cref="RxVal"/> which is only reactive when in Unity Editor.
    /// <para/>
    /// Useful when you want to make something easily inspectable from Unity Editor, so that when you change the values
    /// in Unity inspector you would see things refreshed in the game.
    /// <para/>
    /// While this is useful in the Editor, we don't want such overhead in runtime and thus simply return a wrapped
    /// value.  
    /// </summary>
    public static IRxVal<A> refreshedInEditor<A>(Func<A> calculate) => 
      Application.isEditor 
        ? ObservableU.everyFrame.map(_ => calculate()).toRxVal(calculate()) 
        : RxVal.a(calculate());
  }
}