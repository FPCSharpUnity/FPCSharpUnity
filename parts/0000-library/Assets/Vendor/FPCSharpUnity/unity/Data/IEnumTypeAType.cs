using System;

namespace FPCSharpUnity.unity.Data; 

public interface IEnumTypeAType<in EnumType, SerializedType> where EnumType : unmanaged, Enum {
  SerializedType this[EnumType e] {
    get;
  }

#if UNITY_EDITOR
  void _editor_set(EnumType e, SerializedType value);
#endif
}