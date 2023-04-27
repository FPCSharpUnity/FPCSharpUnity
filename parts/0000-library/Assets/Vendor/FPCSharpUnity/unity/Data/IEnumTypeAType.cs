using System;

namespace FPCSharpUnity.unity.Data; 

public interface IEnumTypeAType<in EnumType, SerializedType> where EnumType : unmanaged, Enum {
  SerializedType this[EnumType e] {
    get;
#if UNITY_EDITOR
    set;
#endif
  }
}