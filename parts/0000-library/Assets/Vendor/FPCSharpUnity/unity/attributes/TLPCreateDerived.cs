using System;
using Sirenix.OdinInspector;

namespace FPCSharpUnity.unity.attributes {
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
  [DontApplyToListElements]
  public class TLPCreateDerivedAttribute : Attribute {}
}