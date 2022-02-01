﻿#if DFGUI && GOTWEEN
using FPCSharpUnity.unity.Iter;
using System.Collections.Generic;
﻿using System.Linq;
﻿using System;
using System.Text.RegularExpressions;
﻿using FPCSharpUnity.unity.Data;
﻿using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
﻿using FPCSharpUnity.unity.Functional;
﻿using FPCSharpUnity.unity.Reactive;using FPCSharpUnity.core.reactive;

﻿using FPCSharpUnity.unity.Tween;

namespace FPCSharpUnity.unity.Binding {
  public static class DFBind {
    private const float TWEEN_DURATION = 0.5f;
    private const GoEaseType TWEEN_EASE = GoEaseType.SineOut;
    private static readonly Regex intFilter = new Regex(@"\D");

    public static readonly Func<string, string> strMapper = _ => _;
    public static readonly Func<int, string> intMapper = _ => _.ToString();
    public static readonly Func<string, int> intComapper = text => {
      var filtered = intFilter.Replace(text, "");
      return filtered.Length == 0 ? 0 : int.Parse(filtered);
    };
    public static readonly Func<uint, string> uintMapper = v => v.ToString();
    public static readonly Func<string, uint> uintComapper = text => {
      var filtered = intFilter.Replace(text, "");
      return filtered.Length == 0 ? 0 : uint.Parse(filtered);
    };

    private static GoTweenConfig tCfg { get {
      return new GoTweenConfig().setEaseType(TWEEN_EASE);
    } }

    /*********** Misc ***********/

    public static void SetIsActive(this dfControl control, bool value) {
      control.IsEnabled = value;
      control.IsVisible = value;
    }

    private static ISubscription withTween(
      Func<Action<GoTween>, ISubscription> body
    ) {
      var tween = F.none<GoTween>();
      return body(newT => {
        tween.each(t => t.destroy());
        tween = Some.a(newT);
      }).andThen(() => tween.each(t => t.destroy()));
    }

    /*********** Observable constructors ***********/

    public static IRxObservable<Tpl<A, dfMouseEventArgs>> clicksObservable<A>(
      this A button
    ) where A : dfControl {
      return new Observable<Tpl<A, dfMouseEventArgs>>(observer => {
        MouseEventHandler dlg = (control, @event) =>
          observer.push(Tpl.a((A) control, @event));
        button.Click += dlg;
        return new Subscription(() => button.Click -= dlg);
      });
    }

    /*********** One-way binds ***********/

    public static ISubscription bind<A>(
      this RxList<A> list, int max, string maxName,
      Func<int, IRxObservable<Option<A>>, ISubscription> bindObservable
    ) {
      var subscription = list.rxSize.subscribe(tracker, size => {
        if (size > max) throw new Exception(String.Format(
          "Max {0} {1} are supported in view, " +
          "but list size was exceeded.", max, maxName
        ));
      });
      var enumeration = Enumerable.Range(0, max).Select(i => {
        var observable = list.rxElement(i);
        return bindObservable(i, observable);
      }).ToList(); /** Eagerly evaluate to bind **/

      return new Subscription(() => {
        subscription.unsubscribe();
        foreach (var s in enumeration) s.unsubscribe();
      });
    }

    public static ISubscription bind<A, Control>(
      this RxList<A> list, int max, string maxName,
      Func<int, Control> getControl, Action<Control, A> onChange
    ) where Control : dfControl {
      return list.bind(max, maxName, (i, observable) => {
        var control = getControl(i);
        return observable.subscribe(tracker, opt => {
          control.SetIsActive(opt.isSome);
          opt.each(v => onChange(control, v));
        });
      });
    }

    public static ISubscription bind(
      this IRxObservable<ValueWithStorage> subject, dfProgressBar control
    ) {
      return withTween(set => subject.subscribe(tracker, value => {
        control.MinValue = 0;
        // 0 out of 0 yields full progress bar which is not what we want.
        control.MaxValue = value.value == 0 && value.storage == 0
          ? 1 : value.storage;
        set(Go.to(
          TF.a(() => control.Value, v => control.Value = v),
          TWEEN_DURATION, tCfg.floatProp(TF.Prop, value.value)
        ));
      }));
    }

    public static ISubscription bind(
      this IRxObservable<ValueWithStorage> subject, dfLabel control
    ) {
      return withTween(set => subject.subscribe(tracker, value => set(Go.to(
        TF.a(
          () => ValueWithStorage.parse(control.Text).AsVector2(),
          v => control.Text = new ValueWithStorage(v).AsString()
        ), TWEEN_DURATION, tCfg.vector2Prop(TF.Prop, value.AsVector2())
      ))));
    }

    public static ISubscription bind(
      this IRxObservable<uint> subject, dfLabel control
    ) {
      return withTween(set => subject.subscribe(tracker, value => {
        set(Go.to(
          TF.a(
            () => (int) uintComapper(control.Text),
            v => control.Text = v.ToString()
          ), TWEEN_DURATION, tCfg.intProp(TF.Prop, (int) value)
        ));
        control.Text = value.ToString();
      }));
    }

    /*********** Two-way binds ***********/

    public static ISubscription bind<T>(
      this IRxRef<T> subject, IEnumerable<dfCheckbox> checkboxes,
      Func<T, string> mapper, Func<string, T> comapper
    ) {
      var optSubject = RxRef.a(Some.a(subject.value));
      var optSubjectSourceSubscription = subject.subscribe(tracker, v =>
        optSubject.value = Some.a(v)
      );
      var optSubjectTargetSubscription = optSubject.subscribe(tracker, opt =>
        opt.each(v => subject.value = v)
      );

      var bindSubscription = optSubject.bind(checkboxes, mapper, comapper);
      return new Subscription(() => {
        optSubjectSourceSubscription.unsubscribe();
        optSubjectTargetSubscription.unsubscribe();
        bindSubscription.unsubscribe();
      });
    }

    public static ISubscription bind<T>(
      this IRxRef<Option<T>> subject, IEnumerable<dfCheckbox> checkboxes,
      Func<T, string> mapper, Func<string, T> comapper
    ) {
      Action uncheckAll = () => {
        foreach (var cb in checkboxes) cb.IsChecked = false;
      };
      Action<Option<T>, string> check = (v, name) =>
        checkboxes.hIter().find(cb => cb.name == name).voidFold(
          () => {
            throw new Exception(String.Format(
              "Can't find checkbox with name {0} which was mapped from {1}",
              name, v
            ));
          },
          cb => cb.IsChecked = true
        );

      uncheckAll();
      subject.value.map(mapper).each(name => check(subject.value, name));

      var subscription = subject.subscribe(tracker, v =>
        v.map(mapper).voidFold(uncheckAll, name => check(v, name))
      );
      PropertyChangedEventHandler<bool> handler = (control, selected) => {
        if (selected) subject.value = Some.a(comapper(control.name));
      };

      foreach (var cb in checkboxes) cb.CheckChanged += handler;

      return new Subscription(() => {
        subscription.unsubscribe();
        foreach (var cb in checkboxes) cb.CheckChanged -= handler;
      });
    }

    public static ISubscription bind(
      this IRxRef<string> subject, dfTextbox control
    ) {
      return subject.bind(control, strMapper, strMapper);
    }

    public static ISubscription bind(
      this IRxRef<int> subject, dfTextbox control
    ) {
      return subject.bind(control, intMapper, intComapper);
    }

    public static ISubscription bind(
      this IRxRef<uint> subject, dfTextbox control
    ) {
      return subject.bind(control, uintMapper, uintComapper);
    }

    public static ISubscription bind<T>(
      this IRxRef<T> subject, dfTextbox control,
      Func<T, string> mapper, Func<string, T> comapper
    ) {
      return subject.bind(
        mapper, comapper,
        text => control.Text = text,
        handler => control.TextChanged += handler,
        handler => control.TextChanged -= handler
      );
    }

    public static ISubscription bind(
      this IRxRef<string> subject, dfLabel control
    ) {
      return subject.bind(control, strMapper, strMapper);
    }

    public static ISubscription bind(
      this IRxRef<int> subject, dfLabel control
    ) {
      return subject.bind(control, intMapper, intComapper);
    }

    public static ISubscription bind(
      this IRxRef<uint> subject, dfLabel control
    ) {
      return subject.bind(control, uintMapper, uintComapper);
    }

    public static ISubscription bind<T>(
      this IRxRef<T> subject, dfLabel control,
      Func<T, string> mapper, Func<string, T> comapper
    ) {
      return subject.bind(
        mapper, comapper,
        text => control.Text = text,
        handler => control.TextChanged += handler,
        handler => control.TextChanged -= handler
      );
    }

    public static ISubscription bind<T>(
      this IRxRef<T> subject,
      Func<T, string> mapper, Func<string, T> comapper,
      Action<string> changeControlText,
      Action<PropertyChangedEventHandler<string>> subscribeToControlChanged,
      Action<PropertyChangedEventHandler<string>> unsubscribeToControlChanged
    ) {
      var f = mapper.andThen(changeControlText);
      var subscription = subject.subscribe(f);
      PropertyChangedEventHandler<string> handler =
        (c, value) => subject.value = comapper(value);
      subscribeToControlChanged(handler);
      return new Subscription(() => {
        unsubscribeToControlChanged(handler);
        subscription.unsubscribe();
      });
    }
  }
}
#endif