using System;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.DebugConsole;

public partial class DConsole {
  [Record] public readonly partial struct LogEntry {
    public readonly DateTime createdAt;
    public readonly string message;
    public readonly LogType type;
  }
}