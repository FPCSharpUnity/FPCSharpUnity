using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.core.utils;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.DebugConsole {
  [PublicAPI] public readonly struct DConsoleRegistrar {
    readonly DConsole.Commands console;
    
    /// <summary>Name of the command group for which this registrar is for.</summary>
    public readonly DConsole.GroupName commandGroup;
    
    internal DConsoleRegistrar(
      DConsole.Commands console, DConsole.GroupName commandGroup
    ) {
      this.console = console;
      this.commandGroup = commandGroup;
    }

    static readonly HasObjFunc<Unit> unitSomeFn = () => Some.a(F.unit);

    public ISubscription register(
      string name, Action run, KeyCodeWithModifiers? shortcut = null, Func<bool> canShow = null
    ) => register(name, api => run(), shortcut, canShow);
    public ISubscription register(
      string name, Action<DConsoleCommandAPI> run, KeyCodeWithModifiers? shortcut = null, Func<bool> canShow = null
    ) => register(name, api => { run(api); return F.unit; }, shortcut, canShow);
    public ISubscription register<A>(
      string name, Func<A> run, KeyCodeWithModifiers? shortcut = null, Func<bool> canShow = null
    ) => register(name, api => run(), shortcut, canShow);
    public ISubscription register<A>(
      string name, Func<DConsoleCommandAPI, A> run, KeyCodeWithModifiers? shortcut = null, Func<bool> canShow = null
    ) => register(name, unitSomeFn, (api, _) => run(api), shortcut, canShow);
    public ISubscription register<A>(
      string name, Func<DConsoleCommandAPI, Future<A>> run, KeyCodeWithModifiers? shortcut = null,
      Func<bool> canShow = null
    ) => register(name, unitSomeFn, (api, _) => run(api), shortcut, canShow);
    public ISubscription register<Obj>(
      string name, HasObjFunc<Obj> objOpt, Action<Obj> run, KeyCodeWithModifiers? shortcut = null,
      Func<bool> canShow = null
    ) => register(name, objOpt, (api, obj) => run(obj), shortcut, canShow);
    public ISubscription register<Obj>(
      string name, HasObjFunc<Obj> objOpt, Action<DConsoleCommandAPI, Obj> run, KeyCodeWithModifiers? shortcut = null,
      Func<bool> canShow = null
    ) => register(name, objOpt, (api, obj) => { run(api, obj); return F.unit; }, shortcut, canShow);
    public ISubscription register<Obj, A>(
      string name, HasObjFunc<Obj> objOpt, Func<Obj, A> run, KeyCodeWithModifiers? shortcut = null,
      Func<bool> canShow = null
    ) => register(name, objOpt, (api, obj) => run(obj), shortcut, canShow);
    public ISubscription register<Obj, A>(
      string name, HasObjFunc<Obj> objOpt, Func<DConsoleCommandAPI, Obj, A> run, KeyCodeWithModifiers? shortcut = null,
      Func<bool> canShow = null
    ) => register(name, objOpt, (api, obj) => Future.successful(run(api, obj)), shortcut, canShow);
    public ISubscription register<Obj, A>(
      string name, HasObjFunc<Obj> objOpt, Func<Obj, Future<A>> run, KeyCodeWithModifiers? shortcut = null,
      Func<bool> canShow = null
    ) => register(name, objOpt, (api, obj) => run(obj), shortcut, canShow);
    public ISubscription register<Obj, A>(
      string name, HasObjFunc<Obj> objOpt, Func<DConsoleCommandAPI, Obj, Future<A>> run, KeyCodeWithModifiers? shortcut = null,
      Func<bool> canShow = null
    ) {
      var prefixedName = $"[DC|{s(commandGroup)}]> {s(name)}";
      return console.register(new DConsole.Command(commandGroup, name, shortcut.toOption(), api => {
        var opt = objOpt();
        if (opt.valueOut(out var obj)) {
          var returnFuture = run(api, obj);

          void onComplete(A t) => Debug.Log($"{s(prefixedName)} done: {t}");
          // Check perhaps it is completed immediately.
          if (returnFuture.value.valueOut(out var a)) onComplete(a);
          else {
            Debug.Log($"{s(prefixedName)} starting.");
            returnFuture.onComplete(onComplete);
          }
        }
        else Debug.Log($"{s(prefixedName)} not running: {typeof(Obj)} is None.");
      }, canShow ?? (() => true)));
    }

    public void registerToggle(
      string name, Ref<bool> r, string comment=null,
      KeyCodeWithModifiers? shortcut=null,
      Func<bool> canShow = null
    ) =>
      registerToggle(name, () => r.value, v => r.value = v, comment, shortcut, canShow);

    public void registerToggle(
      string name, Func<bool> getter, Action<bool> setter, string comment=null,
      KeyCodeWithModifiers? shortcut=null, Func<bool> canShow = null
    ) {
      register($"{name}?", getter, canShow: canShow);
      register($"Toggle {name}", shortcut: shortcut, run: () => {
        setter(!getter());
        return comment == null ? getter().ToString() : $"{comment}: value={getter()}";
      }, canShow: canShow);
    }
    
    public void registerToggleOpt(
      string name, Ref<Option<bool>> r, string comment=null,
      KeyCodeWithModifiers? shortcut=null, Func<bool> canShow = null
    ) {
      register($"{name}?", () => r.value, canShow: canShow);
      register($"Clear {name}", () => r.value = None._, canShow: canShow);
      register($"Toggle {name}", shortcut: shortcut, canShow: canShow, run: () => {
        var current = r.value.getOrElse(false);
        r.value = Some.a(!current);
        return comment == null ? r.value.ToString() : $"{comment}: value={r.value}";
      });
    }

    public void registerNumeric<A>(
      string name, Ref<A> a, PlusMaybeMinus<A> num, A step,
      ImmutableList<A> quickSetValues = null, Func<bool> canShow = null
    ) {
      register($"{name}?", () => a.value, canShow: canShow);
      register($"{name} += {step}", () => a.value = num.add(a.value, step), canShow: canShow);
      register($"{name} -= {step}", () => {
        if (num.subtract(a.value, step).valueOut(out var newValue)) a.value = newValue;
      }, canShow: canShow);
      if (quickSetValues != null) {
        foreach (var value in quickSetValues)
          register($"{name} = {value}", () => a.value = value, canShow: canShow);
      }
    }

    public void registerNumeric<A>(
      string name, Ref<A> a, Numeric<A> num,
      ImmutableList<A> quickSetValues = null, Func<bool> canShow = null
    ) =>
      registerNumeric(name, a, num, num.fromInt(1), quickSetValues, canShow: canShow);

    public void registerNumericOpt<A>(
      string name, Ref<Option<A>> aOpt, A showOnNone, PlusMaybeMinus<A> plusMinus, A step,
      ImmutableList<A> quickSetValues = null, Func<bool> canShow = null
    ) {
      register($"Clear {name}", () => aOpt.value = None._, canShow: canShow);
      register($"{name} opt?", () => aOpt.value, canShow: canShow);
      registerNumeric(
        name, Ref.a(
          () => aOpt.value.getOrElse(showOnNone),
          v => aOpt.value = Some.a(v)
        ), plusMinus, step, quickSetValues, canShow: canShow
      );
    }

    public void registerNumericOpt<A>(
      string name, Ref<Option<A>> aOpt, A showOnNone, Numeric<A> num,
      ImmutableList<A> quickSetValues = null, Func<bool> canShow = null
    ) => registerNumericOpt(name, aOpt, showOnNone, num, num.fromInt(1), quickSetValues, canShow);

    public void registerCountdown(
      string name, uint count, Action run, KeyCodeWithModifiers? shortcut = null, Func<bool> canShow = null
    ) => registerCountdown(name, count, api => run(), shortcut, canShow);
    
    public void registerCountdown(
      string name, uint count, Action<DConsoleCommandAPI> run, KeyCodeWithModifiers? shortcut=null, Func<bool> canShow = null
    ) =>
      register(name, shortcut: shortcut, run: countdownAction(count, run), canShow: canShow);

    /// <summary>
    /// Creates an action for that you can register that only executes after certain amount of invocations. 
    /// </summary>
    public Func<DConsoleCommandAPI, string> countdownAction(uint count, Action<DConsoleCommandAPI> runOnCountdown) {
      var f = countdownActionObj<Unit>(count, (api, obj) => runOnCountdown(api));
      return api => f(api, Unit._);
    }

    /// <summary>
    /// As <see cref="countdownAction"/> but allows to check for <see cref="HasObjFunc{Obj}"/>.
    /// </summary>
    public Func<DConsoleCommandAPI, Obj, string> countdownActionObj<Obj>(uint count, Action<DConsoleCommandAPI, Obj> runOnCountdown) {
      var countdown = count;
      return (api, obj) => {
        countdown--;
        if (countdown == 0) {
          runOnCountdown(api, obj);
          countdown = count;
          return "EXECUTED.";
        }
        return $"Press me {countdown} more times to execute.";
      };
    }

    public void registerEnum<A>(
      string name, Ref<A> reference, IEnumerable<A> enumerable, string comment = null, Func<bool> canShow = null
    ) {
      register($"{name}?", () => {
        var v = reference.value;
        return comment == null ? v.ToString() : $"{comment}: value={v}";
      }, canShow: canShow);
      foreach (var a in enumerable)
        register($"{name}={a}", () => {
          reference.value = a;
          return comment == null ? a.ToString() : $"{comment}: value={a}";
        }, canShow: canShow);
    }
    
    /// <summary>
    /// As <see cref="registerEnum{A}"/>, but registers an additional action to clear the value.
    /// </summary>
    public void registerEnumOpt<A>(
      string name, Ref<Option<A>> reference, IEnumerable<A> enumerable, string comment = null, Func<bool> canShow = null
    ) {
      register($"{name}?", () => {
        var v = reference.value;
        return comment == null ? v.ToString() : $"{comment}: value={v}";
      }, canShow: canShow);
      register($"Clear {name}", () => reference.value = None._, canShow: canShow);
      foreach (var a in enumerable)
        register($"{name}={a}", () => {
          reference.value = Some.a(a);
          return comment == null ? a.ToString() : $"{comment}: value={a}";
        }, canShow: canShow);
    }

    public delegate bool IsSet<in A>(A value, A flag);
    public delegate A Set<A>(A value, A flag);
    public delegate A Unset<A>(A value, A flag);

    /// <param name="name"></param>
    /// <param name="reference"></param>
    /// <param name="isSet">Always <code>(value, flag) => (value & flag) != 0</code></param>
    /// <param name="set">Always <code>(value, flag) => value | flag</code></param>
    /// <param name="unset">Always <code>(value, flag) => value & ~flag</code></param>
    /// <param name="comment"></param>
    /// <param name="canShow"></param>
    public void registerFlagsEnum<A>(
      string name, Ref<A> reference,
      IsSet<A> isSet, Set<A> set, Unset<A> unset,
      string comment = null, Func<bool> canShow = null
    ) where A : Enum {
      var values = EnumUtils.GetValues<A>();
      register(
        $"{name}?", 
        () => 
          values
          .Select(a => $"{a}={isSet(reference.value, a)}")
          .OrderBySafe(_ => _)
          .mkString(", "),
        canShow: canShow
      );
      register(
        $"Set all {name}",
        () => reference.value = values.Aggregate(reference.value, (c, a) => set(c, a)), canShow: canShow
      );
      register(
        $"Clear all {name}",
        () => reference.value = values.Aggregate(reference.value, (c, a) => unset(c, a)), canShow: canShow
      );
      foreach (var a in values) {
        register($"{name}: toggle {a}", canShow: canShow, run: () =>
          reference.value = 
            isSet(reference.value, a) 
              ? unset(reference.value, a) 
              : set(reference.value, a)
        );
      }
    }

    public static readonly ImmutableArray<bool> BOOLS = ImmutableArray.Create(true, false);
    static readonly Option<bool>[] OPT_BOOLS = {F.none<bool>(), Some.a(false), Some.a(true)};
    
    public void registerBools(
      string name, Ref<bool> reference, string comment = null, Func<bool> canShow = null
    ) => registerEnum(name, reference, BOOLS, comment, canShow);
    
    public void registerBools(
      string name, Ref<Option<bool>> reference, string comment = null, Func<bool> canShow = null
    ) => registerEnum(name, reference, OPT_BOOLS, comment, canShow);
  }
}