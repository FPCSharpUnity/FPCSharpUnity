using FPCSharpUnity.core.dispose;
using FPCSharpUnity.unity.Components.ui;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Pools;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.DebugConsole;

public partial class DConsole {
  // DO NOT generate comparer and hashcode - we need reference equality for dynamic vertical layout!
  [Record(GenerateComparer = false, GenerateGetHashCode = false)]
  public partial class DynamicVerticalLayoutLogElementData : 
    DynamicLayout.ElementBase<VerticalLayoutLogEntry.Data, VerticalLayoutLogEntry> 
  {
    public DynamicVerticalLayoutLogElementData(
      GameObjectPool<VerticalLayoutLogEntry> pool, VerticalLayoutLogEntry.Data data
    ) : base(
      data, sizeProvider: new DynamicLayout.SizeProvider.Static(20f, new Percentage(1f)), 
      maybeViewProvider: DynamicLayout.ViewProvider.pooled(pool), log
    ) { }

    protected override void becameVisible(VerticalLayoutLogEntry view, RectTransform rt, RectTransform parent) {
      base.becameVisible(view, rt, parent);
      rt.SetParent(parent, false);
    }

    protected override void updateState(VerticalLayoutLogEntry view, ITracker tracker) => view.updateState(data);
  }
}