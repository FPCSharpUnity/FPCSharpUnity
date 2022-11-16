﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExhaustiveMatching;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.pools;
using UnityEngine;

namespace FPCSharpUnity.unity.Concurrent {
  public sealed class UnityCoroutine : CustomYieldInstruction, ICoroutine {
    public CoroutineState state { get; private set; } = CoroutineState.Running;

    public override bool keepWaiting => state.isRunning();

    bool shouldStop;

    event CoroutineFinishedOrStopped _onFinish;
    public event CoroutineFinishedOrStopped onFinish {
      add {
        switch (state) {
          case CoroutineState.Running:
            _onFinish += value;
            break;
          case CoroutineState.Stopped:
            value(finished: false);
            break;
          case CoroutineState.Finished:
            value(finished: true);
            break;
          default:
            throw ExhaustiveMatch.Failed(state);
        }
      }
      remove { _onFinish -= value; }
    }

    public UnityCoroutine(
      MonoBehaviour behaviour, IEnumerator enumerator,
      [CallerFilePath] string callerFilePath = "",
      [CallerMemberName] string callerMemberName = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) {
      var fixBugsEnumerator = fixUnityBugs(enumerator);
      if (Application.isPlaying) {
        behaviour.StartCoroutine(fixBugsEnumerator);
      } 
#if UNITY_EDITOR
      else {
        // This is a hack to run coroutine in edit mode, no other yield instructions
        // beside null are supported.
        void updateFn() {
          // ReSharper disable once DelegateSubtraction
          void unsubscribe() => UnityEditor.EditorApplication.update -= updateFn;
          
          var hasNext = fixBugsEnumerator.MoveNext();
          if (!behaviour || !hasNext) unsubscribe();
          if (hasNext) {
            var yieldInstruction = fixBugsEnumerator.Current;
            if (yieldInstruction != null) {
              unsubscribe();
              Log.d.error(
                $"Aborting coroutine started in {callerMemberName} @ {callerFilePath}:{callerLineNumber}, " +
                $"because it yielded {yieldInstruction}, which we do not know how to fake to in editor!"
              );
            }
          }
        }
        UnityEditor.EditorApplication.update += updateFn;
      }
#endif
    }

    static readonly Pool<Stack<IEnumerator>> stackPool = new Pool<Stack<IEnumerator>>(
      () => new Stack<IEnumerator>(),
      s => s.Clear()
    );
    
    /**
     * So...
     *
     * 1. https://fogbugz.unity3d.com/default.asp?826400_tcbicqltkckqmer1
     * 2. Unity API has no way to check whether Coroutine has been completed.
     **/
    IEnumerator fixUnityBugs(IEnumerator startingEnumerator) {
      using var stackDisposable = stackPool.BorrowDisposable();
      var stack = stackDisposable.value;
      stack.Push(startingEnumerator);
        
      while (stack.Count > 0 && !shouldStop) {
        var enumerator = stack.Peek();
        if (enumerator.MoveNext()) {
          var current = enumerator.Current;
          switch (current) {
            case IEnumerator innerEnumerator:
              stack.Push(innerEnumerator);
              break;
            default:
              var isKnown = current == null || current is YieldInstruction;
              if (!isKnown) {
                Log.d.mWarn($"{stack.Count}: {enumerator} yielding unknown {current.GetType()}");
              }

              yield return current;
              break;
          }
        }
        else {
          stack.Pop();
        }
      }

      state = shouldStop ? CoroutineState.Stopped : CoroutineState.Finished;
      _onFinish?.Invoke(finished: !shouldStop);
      _onFinish = null;
    }

    public void stop() => shouldStop = true;

    public void Dispose() => stop();
  }
}
