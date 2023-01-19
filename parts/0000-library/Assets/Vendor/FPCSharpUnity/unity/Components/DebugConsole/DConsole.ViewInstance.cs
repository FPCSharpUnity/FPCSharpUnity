using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Components.ui;
using FPCSharpUnity.unity.Pools;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.DebugConsole;

public partial class DConsole {
  /// <summary>
  /// Data for a currently visible <see cref="DConsole"/> view.
  /// </summary>
  [Record] sealed partial class ViewInstance {
    public readonly DebugConsoleBinding view;
    public readonly DynamicLayout.Init<DynamicVerticalLayoutLogElementData> dynamicVerticalLayout;
    public readonly GameObjectPool<VerticalLayoutLogEntry> pool;
    public readonly IDisposableTracker tracker;

    /// <summary>
    /// Destroys the Unity objects for this view.
    /// </summary>
    public void destroy() {
      log.mInfo("Destroying DConsole.");
      tracker.Dispose();
      pool.dispose(Object.Destroy);
      Object.Destroy(view.gameObject);
    }
  }
}