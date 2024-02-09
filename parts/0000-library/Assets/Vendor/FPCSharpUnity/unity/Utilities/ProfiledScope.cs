using System;
using GenerationAttributes;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace FPCSharpUnity.unity.Utilities {
  /// <summary>
  /// Allows using profiles safely and without garbage creation.
  ///
  /// <code><![CDATA[
  /// using (var _ = new ProfiledScope("scope name")) {
  ///   // your code here
  /// }
  /// ]]></code>
  /// </summary>
  [IgnoredByDeepProfiler]
  public struct ProfiledScope : IDisposable {
    public ProfiledScope(string name) {
      Profiler.BeginSample(name);
    }

    public void Dispose() {
      Profiler.EndSample();
    }
    
    /// <summary>
    /// Same as `new ProfiledScope()` but gets compiled out on release builds.
    /// <code><![CDATA[
    /// using (ProfiledScope.profileM("label") {
    ///   // do stuff
    /// }
    /// ]]></code>
    /// </summary>
    [SimpleMethodMacroScriban(
#if ENABLE_PROFILER
      "new FPCSharpUnity.unity.Utilities.ProfiledScope({{name}})"
#else 
      "null"
#endif
    )]
    public static IDisposable profileM(string name) => throw new MacroException();
  
    /// <summary>
    /// Same as `new ProfiledScope()` but gets compiled out on release builds.
    /// <code><![CDATA[
    /// void method() {
    ///   ProfiledScope.profileUsingM(Macros.classAndMethodNameShort);
    ///   // do stuff
    /// }
    /// ]]></code>
    /// </summary>
    [StatementMethodMacroScriban(
#if ENABLE_PROFILER
      "using var {{uniqueId}} = new FPCSharpUnity.unity.Utilities.ProfiledScope({{name}});"
#else 
      ""
#endif
    )]
    public static IDisposable profileUsingM(string name) => throw new MacroException();
  }
}