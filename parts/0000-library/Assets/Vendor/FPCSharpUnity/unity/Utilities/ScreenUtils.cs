using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Reactive;
using GenerationAttributes;
using FPCSharpUnity.core.reactive;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities {
  public static partial class ScreenUtils {
    public static Size screenSize => new Size(Screen.width, Screen.height);

    [LazyProperty] public static IRxVal<Size> screenSizeVal => 
      ObservableU.everyFrame.toRxVal(() => screenSize);

    [LazyProperty] public static IRxVal<Rect> screenSafeArea => 
      ObservableU.everyFrame.toRxVal(() => Screen.safeArea);

    /// <summary>Convert screen width percentage to absolute value.</summary>
    public static float pWidthToAbs(this float percentWidth) => Screen.width * percentWidth;

    /// <summary>Convert screen height percentage to absolute value.</summary>
    public static float pHeightToAbs(this float percentHeight) => Screen.height * percentHeight;

    /// <summary>Convert screen width absolute value to percentage.</summary>
    public static float aWidthToPerc(this float absoluteWidth) => absoluteWidth / Screen.width;

    /// <summary>Convert screen height absolute value to percentage.</summary>
    public static float aHeightToPerc(this float absoluteHeight) => absoluteHeight / Screen.height;
  }
}