using System;
using System.Text;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Extensions {
  public static class EncodingExts {
    public static Try<string> GetStringTry(this Encoding enc, byte[] bytes) {
      try { return F.scs(enc.GetString(bytes)); }
      catch (Exception e) { return F.err<string>(e); }
    }
  }
}