using System;
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
  }
}