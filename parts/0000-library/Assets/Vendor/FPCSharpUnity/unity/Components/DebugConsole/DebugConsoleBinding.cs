using System;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Components.ui;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.reactive;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.DebugConsole {
  public sealed partial class DebugConsoleBinding : MonoBehaviour, IMB_Update {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [NotNull] public DebugConsoleListBinding commandGroups, commands;
    [NotNull] public Text commandGroupLabel;
    [NotNull] public ButtonBinding buttonPrefab;
    [NotNull] public Button closeButton, minimiseButton;
    [NotNull] public DynamicLayout dynamicLayout;
    [NotNull] public VerticalLayoutLogEntryPrefab logEntry;
    [NotNull] public GameObject logPanel;
    [NotNull, SerializeField, PublicAccessor] GameObject _modals;
    [NotNull, SerializeField, PublicAccessor] DebugConsoleInputModalBinding _inputModal;
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

    public float lineWidth => dynamicLayout.maskRect.rect.width;

    readonly IRxRef<bool> _maximizedRx = RxRef.a(true);
    public IRxVal<bool> maximizedRx => _maximizedRx;

    public void toggleMaximized() {
      _maximizedRx.value = !_maximizedRx.value;
      var active = _maximizedRx.value;
      closeButton.setActiveGO(active);
      commandGroups.setActiveGO(active);
      commands.setActiveGO(active);
      logPanel.SetActive(active);
    }

    public void showModal(bool inputModal = false) {
      _modals.SetActive(true);
      _inputModal.setActiveGO(inputModal);
    }

    public void hideModals() => _modals.SetActive(false);

    public event Action onUpdate;
    public void Update() => onUpdate?.Invoke();
  }

  [Serializable]
  public class VerticalLayoutLogEntryPrefab : TagPrefab<VerticalLayoutLogEntry> { }
}
