using System;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.unity.Data;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Components.DebugConsole;

public partial class DConsole {
  [Record(ConstructorFlags.Withers)] public partial struct Command {
    public readonly GroupName cmdGroup;
    public readonly string name;
    public readonly Option<KeyCodeWithModifiers> shortcut; 
    public readonly Action<DConsoleCommandAPI> run;
    public readonly Func<bool> canShow;

    public string label => shortcut.valueOut(out var sc) ? $"[{Str.s(sc)}] {Str.s(name)}" : name;
  }
}