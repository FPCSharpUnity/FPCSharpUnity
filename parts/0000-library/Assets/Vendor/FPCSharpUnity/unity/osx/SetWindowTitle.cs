// See Utilities.SetWindowTitle.cs for explanation.
#if UNITY_EDITOR_OSX || (UNITY_STANDALONE_OSX && !UNITY_EDITOR)
using System;
using System.Runtime.InteropServices;
using FPCSharpUnity.unity.core.Utilities;
using FPCSharpUnity.core.exts;

namespace FPCSharpUnity.unity.osx {
  public class SetWindowTitle : ISetWindowTitle {
    public bool setWindowTitle(string title) {
      FPCSharpUnityOSXWindowSetTitle(title);
      return true;
    }

    [DllImport("fp_csharp_unity_osx")]
    static extern void FPCSharpUnityOSXWindowSetTitle(string title);
  }
}
#endif