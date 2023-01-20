using ExhaustiveMatching;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.unity.Data;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout {
  /// <summary> Layout part of <see cref="IElement"/>. It provides item's size when doing layout. </summary>
  [Union(
    new[] { typeof(FromTemplateStatic), typeof(Static), typeof(DynamicSizeInScrollableAxis) }
  )]
  public readonly partial struct SizeProvider {
    /// <summary>Height of an element in a vertical layout OR width in horizontal layout</summary>
    public float sizeInScrollableAxis(bool isHorizontal) => __case switch {
      Case.FromTemplateStatic => isHorizontal ? __fromTemplateStatic.rect.width : __fromTemplateStatic.rect.height,
      Case.Static => __static.sizeInScrollableAxis,
      Case.DynamicSizeInScrollableAxis => __dynamicSizeInScrollableAxis.sizeInScrollableAxisVal.value,
      _ => throw ExhaustiveMatch.Failed(__case)
    };
      
    /// <inheritdoc cref="FromTemplateStatic.fullRowOrColumn"/>
    public static FromTemplateStatic fullRowOrColumnFromTemplate(
      Component template
    ) =>
      FromTemplateStatic.fullRowOrColumn(template);
    
    public static Static fullRowOrColumnStatic(float sizeInScrollableAxis) => 
      new Static(sizeInScrollableAxis, sizeInSecondaryAxis: new Percentage(1));
    
    /// <summary> Sample item's size from template object. This size will not change. </summary>
    [Record] public readonly partial struct FromTemplateStatic {
      /// <summary> Item's size. </summary>
      public readonly Rect rect;

      /// <summary>Item width portion of vertical layout width OR height in horizontal layout.</summary>
      public readonly Percentage sizeInSecondaryAxis;

      public FromTemplateStatic(Component o, Percentage sizeInSecondaryAxis) : this(
        ((RectTransform)o.transform).rect, sizeInSecondaryAxis
      ) {}
      
      /// <summary>
      /// Sample <see cref="template"/>'s size for height/width, but put <see cref="sizeInSecondaryAxis"/> as full
      /// row/column (100%).
      /// </summary>
      public static FromTemplateStatic fullRowOrColumn(Component template) =>
        new(template, new Percentage(1));
    }
      
    /// <summary> Define custom item's size. This size will not change. </summary>
    [Record] public readonly partial struct Static {
      public readonly float sizeInScrollableAxis;
      
      /// <summary>Item width portion of vertical layout width OR height in horizontal layout.</summary>
      public readonly Percentage sizeInSecondaryAxis;
    }

    /// <summary> Like <see cref="Static"/>, but the <see cref="sizeInScrollableAxis"/> changes. </summary>
    [Record] public readonly partial struct DynamicSizeInScrollableAxis {
      public readonly Val<float> sizeInScrollableAxisVal;
      
      /// <summary>Item width portion of vertical layout width OR height in horizontal layout.</summary>
      public readonly Percentage sizeInSecondaryAxis;
    }
  }
}