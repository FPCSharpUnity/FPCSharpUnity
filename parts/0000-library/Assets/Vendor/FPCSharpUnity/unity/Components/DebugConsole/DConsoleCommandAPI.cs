using System;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Components.DebugConsole;

[PublicAPI] public interface DConsoleCommandAPI {
  /// <summary>
  /// Shows a modal input dialog.
  /// </summary>
  /// <param name="inputLabel">The label for the input field.</param>
  /// <param name="inputPlaceholder">The value shown until user inputs something.</param>
  /// <param name="button1">First button.</param>
  /// <param name="button2">Second optional button.</param>
  void showModalInput(
    string inputLabel, string inputPlaceholder,
    ButtonData<DConsoleModalInputAPI> button1, Option<ButtonData<DConsoleModalInputAPI>> button2 = default
  );

  /// <summary>
  /// Re-renders all commands, adding newly created commands.
  /// </summary>
  void rerender();
}

class DConsoleCommandAPIImpl : DConsoleCommandAPI {
  readonly DebugConsoleBinding view;
  readonly Action _rerender;

  public DConsoleCommandAPIImpl(DebugConsoleBinding view, Action rerender) {
    this.view = view;
    _rerender = rerender;
  }

  public void showModalInput(
    string inputLabel, string inputPlaceholder, 
    ButtonData<DConsoleModalInputAPI> button1, Option<ButtonData<DConsoleModalInputAPI>> button2 = default
  ) {
    view.showModal(inputModal: true);
    var m = view.inputModal;
    m.label.text = inputLabel;
    m.error.text = "";
    m.inputPlaceholder.text = inputPlaceholder;
    var inputApi = new DConsoleModalInputAPIImpl(view);
    setupButton(m.button1, button1);
    m.button2.button.setActiveGO(button2.isSome);
    { if (button2.valueOut(out var b2)) setupButton(m.button2, b2); }

    void setupButton(ButtonBinding b, ButtonData<DConsoleModalInputAPI> data) {
      b.text.text = data.label;
      b.button.onClick.RemoveAllListeners();
      b.button.onClick.AddListener(() => data.onClick(inputApi));
    }
  }

  public void rerender() => _rerender();
}