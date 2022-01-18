﻿using System;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Extensions {
  public static class ByteArrayExts {
    [PublicAPI]
    public static Try<int> toInt(this byte[] data, int startIndex = 0) {
      try { return F.scs(BitConverter.ToInt32(data, startIndex)); }
      catch (Exception e) { return F.err<int>(e); }
    }

    [PublicAPI]
    public static Try<ushort> toUShort(this byte[] data, int startIndex = 0) {
      try { return F.scs(BitConverter.ToUInt16(data, startIndex)); }
      catch (Exception e) { return F.err<ushort>(e); }
    }

    [PublicAPI]
    public static ulong guidAsULong(this byte[] data) =>
      unchecked (BitConverter.ToUInt64(data, 0) + BitConverter.ToUInt64(data, 8));
  }
}