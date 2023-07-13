using FPCSharpUnity.core.functional;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout {
  /// <summary> Layout part of <see cref="IElement"/>. It provides item's size when doing layout. </summary>
  [Record] public readonly partial struct SizeProvider {
    public readonly SizeInSecondaryAxis itemSizeInSecondaryAxis;
    public readonly SizeInScrollableAxis sizeInScrollableAxis;
    
    /// <summary> Optional space after this item. </summary>
    [
      RecordDefaultValueForConstructorParam
    ] public readonly Option<SizeInSecondaryAxis> spacingAfterItemSizeInSecondaryAxis;
    
    /// <summary>
    /// Sample <see cref="template"/>'s size for height/width, but put <see cref="itemSizeInSecondaryAxis"/> as full
    /// row/column (100%).
    /// </summary>
    public static SizeProvider fullRowOrColumn(Component template) => new(
      SizeInSecondaryAxis.fullRowOrColumn, SizeInScrollableAxis.fromTemplate(template)
    );
    
    public static SizeProvider fullRowOrColumn(SizeInScrollableAxis sizeInScrollableAxis) => new(
      SizeInSecondaryAxis.fullRowOrColumn, sizeInScrollableAxis
    );

    public static SizeProvider fromTemplate(Component c) => new(
      SizeInSecondaryAxis.fromTemplate(c), SizeInScrollableAxis.fromTemplate(c) 
    );

    public static SizeProvider fromTemplate(Component c, SizeInSecondaryAxis sizeInSecondaryAxis) => new(
      sizeInSecondaryAxis, SizeInScrollableAxis.fromTemplate(c) 
    );
  }
}