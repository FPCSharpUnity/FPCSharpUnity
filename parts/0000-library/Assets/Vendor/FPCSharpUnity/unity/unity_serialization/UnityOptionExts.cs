using System;
using GenerationAttributes;

namespace FPCSharpUnity.unity.unity_serialization; 

public static class UnityOptionExts {
  /// <summary>
  /// Checks whether the option is Some and if it is, invokes the provided action on it.
  /// </summary>
  [StatementMethodMacroScriban(FPCSharpUnity.core.exts.OptionExts.FOR_EACH_MACRO)]
  public static void ifSomeM<A>(this UnityOption<A> opt, Action<A> action) => throw new MacroException();
}