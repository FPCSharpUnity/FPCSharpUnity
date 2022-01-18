using System;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Extensions {
  public static class GuidExts {
    [PublicAPI]
    public static ulong asULong(this Guid g) => g.ToByteArray().guidAsULong();
  }
}