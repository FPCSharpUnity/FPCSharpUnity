using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.reactive;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.DebugConsole {
  public sealed partial class DebugConsoleInputModalBinding : MonoBehaviour {
    #region Unity Serialized Fields

#pragma warning disable 649 // ReSharper disable UnassignedField.Global
    // ReSharper disable NotNullMemberIsNotInitialized
    [SerializeField, NotNull, PublicAccessor] Text _label, _inputPlaceholder, _error;
    [SerializeField, NotNull, PublicAccessor] InputField _input;
    [SerializeField, NotNull, PublicAccessor] ButtonBinding _button1, _button2;
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649 // ReSharper restore UnassignedField.Global

    #endregion

    [LazyProperty] IRxObservable<Unit> onButton1Click => _button1.button.uiClick();
    [LazyProperty] IRxObservable<Unit> onButton2Click => _button2.button.uiClick();
  }
}