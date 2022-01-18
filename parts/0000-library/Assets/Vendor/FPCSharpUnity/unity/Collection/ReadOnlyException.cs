using System;

namespace FPCSharpUnity.unity.Collection {
  class ReadOnlyException : Exception {
    public ReadOnlyException(string operation) : base(string.Format(
      "Operation {0} is not supported: collection is read only", operation
    )) {}
  }
}
