#if UNITY_ANDROID
using System;
using FPCSharpUnity.unity.Concurrent;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.app {
  public class AlertDialog : Binding {
    public class Builder : Binding {
      public struct PositiveButton {
        public readonly string text;
        public readonly Action onClick;

        public PositiveButton(string text, Action onClick) {
          this.text = text;
          this.onClick = onClick;
        }
      }

      public Builder(Activity activity = null) : base(new AndroidJavaObject(
        "android.app.AlertDialog$Builder", (activity ?? AndroidActivity.current).java
      )) { }

      public string title { set { java.cjo("setTitle", value); } }
      public string message { set { java.cjo("setMessage", value); } }
      public PositiveButton positiveButton { set { java.cjo(
        "setPositiveButton", value.text, new OnClickListener(value.onClick)
      ); } }
      public bool cancelable { set { java.cjo("setCancelable", value); } }

      public AlertDialog create() => new AlertDialog(java.cjo("create"));
    }

    class OnClickListener : JavaProxy {
      readonly Action callback;

      public OnClickListener(Action callback) : base(
        "android.content.DialogInterface$OnClickListener"
      ) {
        this.callback = callback;
      }

      [UsedImplicitly] void onClick(AndroidJavaObject dialog, int which) {
        dialog.Call("cancel");
        ASync.OnMainThread(callback);
      }
    }

    public AlertDialog(AndroidJavaObject java) : base(java) {}

    public void show() => java.Call("show");
  }
}
#endif