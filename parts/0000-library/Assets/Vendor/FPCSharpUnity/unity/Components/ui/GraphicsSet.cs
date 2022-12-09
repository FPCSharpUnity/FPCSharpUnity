using FPCSharpUnity.unity.Components.Interfaces;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  /// <summary>
  /// Contains multiple <see cref="Graphic"/> components. This component provides an easy and convenient way of changing
  /// multiple <see cref="Graphic"/> properties at once. Currently it's only used for changing color.
  /// </summary>
  public partial class GraphicsSet : MonoBehaviour, IMB_Reset {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [SerializeField, NotNull, PublicAccessor] Graphic[] _graphics;
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

    public void Reset() => _graphics = GetComponentsInChildren<Graphic>();
    
    public Color color { set {
      foreach (var graphic in _graphics) {
        graphic.color = value;
      }      
    } }
  }
}