#if UNITY_ANDROID
using System;
using FPCSharpUnity.unity.Android.Bindings.android.content;
using FPCSharpUnity.unity.Android.java.lang;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.app {
  [JavaBinding("android.app.Activity"), PublicAPI]
  public class Activity : Context {
    public Activity(AndroidJavaObject java) : base(java) {}

    public Application application => 
      new Application(java.cjo("getApplication"));
    
    public void runOnUIThread(Action action) =>
      java.Call("runOnUiThread", new Runnable(action));
    
    public Intent getIntent() =>
      new Intent(java.cjo("getIntent"));
  }
}
#endif