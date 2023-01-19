using FPCSharpUnity.core.reactive;
using JetBrains.Annotations;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static partial class ToggleExts {
    /// <summary>Returns an event stream of <see cref="Toggle.isOn"/> changes.</summary>
    public static IRxObservable<bool> isOnChanges(this Toggle toggle) =>
      Observable.fromEvent2<bool, UnityAction<bool>>(
        registerCallback: push => {
          var callback = new UnityAction<bool>(push);
          toggle.onValueChanged.AddListener(callback);
          return callback;
        },
        unregisterCallback: callback => toggle.onValueChanged.RemoveListener(callback)
      );

    /// <summary>Returns a reactive version of <see cref="Toggle.isOn"/>.</summary>
    public static IRxVal<bool> isOnRx(this Toggle toggle, IRxObservableToIRxValMode mode) =>
      toggle.isOnChanges().toRxVal(mode, toggle.isOn);
  }
}