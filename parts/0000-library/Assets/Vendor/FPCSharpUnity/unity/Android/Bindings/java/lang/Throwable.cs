#if UNITY_ANDROID
using FPCSharpUnity.unity.Android.Bindings.java.io;
using FPCSharpUnity.core.exts;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.java.lang {
  public class Throwable : Binding {
    public Throwable(AndroidJavaObject java) : base(java) {}
    public Throwable(string message) : this(new AndroidJavaObject("java.lang.Throwable", message)) {}

    public string stacktraceString { get {
      var sw = new StringWriter();
      printStackTrace(new PrintWriter(sw));
      return sw.ToString();
    } }

    public void printStackTrace(PrintWriter s) => java.Call("printStackTrace", s.java);

    public string message => java.Call<string>("getMessage");

    public StackTraceElement[] getStackTrace() =>
      java.Call<AndroidJavaObject[]>("getStackTrace")
      .map(ajo => new StackTraceElement(ajo));
  }
}
#endif