using System.Collections.Generic;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities.Editor {
  [Record]
  public partial class UniqueValueScriptableObjectWithList : ScriptableObject {
    public List<ObjectValidatorTest.UniqueValueStruct> listOfStructs;
  }
}