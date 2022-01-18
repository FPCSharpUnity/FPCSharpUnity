using System;
using System.Collections.Generic;
using FPCSharpUnity.unity.Cryptography;
using JetBrains.Annotations;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger.Reporting {
  [PublicAPI] public class LogEventCollector {
    public readonly uint limit;
    
    readonly List<string> _events = new List<string>();
    readonly HashSet<string> hashes = new HashSet<string>();

    public IEnumerable<string> events => _events;

    public LogEventCollector(uint limit) {
      this.limit = limit;
    }

    public void onEvent(LogEvent data) {
      if (_events.Count >= limit) return;
      
      var str = data.ToString();
      var hash = CryptoHash.calculate(str, CryptoHash.Kind.MD5).asString();
      if (hashes.Add(hash)) {
        _events.Add($"{DateTime.Now} {str}");
      }
    }
  }
}