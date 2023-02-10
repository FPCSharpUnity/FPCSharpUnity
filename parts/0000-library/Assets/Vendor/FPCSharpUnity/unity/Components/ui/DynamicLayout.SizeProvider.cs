using ExhaustiveMatching;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.unity.Data;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout {
  /// <summary> Layout part of <see cref="IElement"/>. It provides item's size when doing layout. </summary>
  [Union(new[] {
    typeof(FromTemplateStatic), typeof(Static), typeof(DynamicSizeInScrollableAxis), 
    typeof(FromTemplateWithCustomSizeInSecondaryAxis)
  })]
  public readonly partial struct SizeProvider {
    /// <summary>Height of an element in a vertical layout OR width in horizontal layout</summary>
    public float sizeInScrollableAxis(bool isHorizontal) => __case switch {
      Case.FromTemplateStatic => isHorizontal ? __fromTemplateStatic.rect.width : __fromTemplateStatic.rect.height,
      Case.Static => __static.sizeInScrollableAxis,
      Case.DynamicSizeInScrollableAxis => __dynamicSizeInScrollableAxis.sizeInScrollableAxisVal.value,
      Case.FromTemplateWithCustomSizeInSecondaryAxis => __fromTemplateWithCustomSizeInSecondaryAxis.sizeInScrollableAxis,
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
    
    /// <summary> Sample item's size from template object. This size in secondary axis can change. </summary>
    public readonly struct FromTemplateWithCustomSizeInSecondaryAxis {
      /// <summary> Item's size. </summary>
      public readonly Rect rect;

      readonly bool isHorizontal;

      public readonly Val<float> viewportSizeVal;
      readonly float spacingInSecondaryAxis;

      public Percentage sizeInSecondaryAxis { get {
        var itemWidth = isHorizontal ? rect.height : rect.width;
        return new Percentage((itemWidth + spacingInSecondaryAxis) / viewportSizeVal.value);
      } }

      public float sizeInScrollableAxis => isHorizontal ? rect.width : rect.height;

      public FromTemplateWithCustomSizeInSecondaryAxis(
        Component o, bool isHorizontal, Val<float> viewportSizeVal, float spacingInSecondaryAxis
      ) {
        rect = ((RectTransform)o.transform).rect;
        this.isHorizontal = isHorizontal;
        this.viewportSizeVal = viewportSizeVal;
        this.spacingInSecondaryAxis = spacingInSecondaryAxis;
      }
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