using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Components.DebugConsole;

[PublicAPI] public interface DConsoleModalInputAPI {
  /// <summary>Allows you to get or set the text which is in the input box (where user typed his input in).</summary>
  public string inputText { get; set; }
    
  /// <summary>Allows you to get or set the text which is shown as an error.</summary>
  public string errorText { get; set; }
    
  /// <summary>Closes the modal dialog.</summary>
  public void closeDialog();
}

class DConsoleModalInputAPIImpl : DConsoleModalInputAPI {
  readonly DebugConsoleBinding view;

  public DConsoleModalInputAPIImpl(DebugConsoleBinding view) => this.view = view;

  public string inputText { get => view.inputModal.input.text; set => view.inputModal.input.text = value; }
  public string errorText { get => view.inputModal.error.text; set => view.inputModal.error.text = value; }
  public void closeDialog() => view.hideModals();
}