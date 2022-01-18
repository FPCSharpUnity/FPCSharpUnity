﻿using System;

namespace FPCSharpUnity.unity.Data {
  public class TimeoutException : Exception {
    public readonly Duration timeout;

    public TimeoutException(Duration timeout, string message="Timed out") : base($"{message}: {timeout}") {
      this.timeout = timeout;
    }
  }
}
