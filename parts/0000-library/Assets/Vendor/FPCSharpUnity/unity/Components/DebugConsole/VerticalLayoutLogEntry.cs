using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.DebugConsole {
  public partial class VerticalLayoutLogEntry : MonoBehaviour {
    #region Unity Serialized Fields

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
    [SerializeField, NotNull] RectTransform baseTransform;
    [SerializeField, NotNull] Text text;
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649

    #endregion

    [Record] public readonly partial struct Data {
      public readonly string text;
      public readonly Color color;
    }

    public void updateState(Data data) {
      text.text = data.text;
      text.color = data.color;      
    }
  }
}