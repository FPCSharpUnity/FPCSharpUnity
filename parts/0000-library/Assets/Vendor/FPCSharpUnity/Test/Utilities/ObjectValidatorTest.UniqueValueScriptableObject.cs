using FPCSharpUnity.core.validations;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities.Editor {
  [Record]
  public partial class UniqueValueScriptableObject : ScriptableObject {
    [UniqueValue(ObjectValidatorTest.UNIQUE_CATEGORY)] public byte[] identifier;
    [UniqueValue(ObjectValidatorTest.UNIQUE_CATEGORY)] public byte[] identifier2;
  }
}