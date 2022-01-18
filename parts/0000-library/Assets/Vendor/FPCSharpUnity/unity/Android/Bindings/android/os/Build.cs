﻿#if UNITY_ANDROID
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.os {
  public static class Build {
    public static class Version {
      static readonly AndroidJavaClass klass = new AndroidJavaClass("android.os.Build$VERSION");

      const int MARSHMALLOW_SDK_INT = 23;

      public static readonly string SDK = klass.GetStatic<string>("SDK");
      public static readonly int SDK_INT = klass.GetStatic<int>("SDK_INT");

      public static bool marshmallowOrHigher => SDK_INT >= MARSHMALLOW_SDK_INT;
    }

    static readonly AndroidJavaClass klass = new AndroidJavaClass("android.os.Build");
    public static readonly string MANUFACTURER = klass.GetStatic<string>("MANUFACTURER");
    public static readonly string DEVICE = klass.GetStatic<string>("DEVICE");
  }
}
#endif