﻿#if UNITY_ANDROID
using System.Collections.Generic;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.java.util {
  public class ArrayList : List {
    public ArrayList(AndroidJavaObject java) : base(java) {}
    public ArrayList(int capacity)
      : this(new AndroidJavaObject("java.util.ArrayList", capacity)) {}
    public ArrayList() : this(0) {}

    public ArrayList(IEnumerable<AndroidJavaObject> enumerable, int capacity = 0) : this(capacity) {
      foreach (var elem in enumerable) add(elem);
    }
  }
}
#endif