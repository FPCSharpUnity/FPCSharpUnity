﻿#if UNITY_ANDROID
using System.Text.RegularExpressions;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.exts;
using System;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.java.lang {
  public class StackTraceElement : Binding {
    public StackTraceElement(AndroidJavaObject java) : base(java) {}

    public StackTraceElement(
      string declaringClass, string methodName, string fileName, int lineNumber
    ) : this(new AndroidJavaObject(
      "java.lang.StackTraceElement", declaringClass, methodName, fileName, lineNumber
    )) {}
  }

  public static class StackTraceElementExts {
    public static StackTraceElement asAndroid(this BacktraceElem e) => new(
      declaringClass: e.method.methodAsAndroid(), methodName: "_",
      fileName: e.fileInfo.foldM((string) null, fi => fi.file.ToString()),
      lineNumber: e.fileInfo.foldM(-1, fi => fi.lineNo)
    );

    // The "Java letters" include uppercase and lowercase ASCII Latin
    // letters A-Z (\u0041-\u005a), and a-z (\u0061-\u007a), and, for historical
    // reasons, the ASCII underscore (_, or \u005f) and dollar sign ($, or \u0024).
    // The $ character should be used only in mechanically generated source code or,
    // rarely, to access pre-existing names on legacy systems.
    //
    // The "Java digits" include the ASCII digits 0-9 (\u0030-\u0039).
    static readonly Regex javaDeclaringClassRegex = new(@"[^A-Za-z_\.$0-9]");

    public static string methodAsAndroid(this ReadOnlyMemory<char> method) =>
      javaDeclaringClassRegex.Replace(method.ToString(), "$");
  }
}
#endif