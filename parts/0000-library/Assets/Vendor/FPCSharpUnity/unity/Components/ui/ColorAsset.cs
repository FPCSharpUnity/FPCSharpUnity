using FPCSharpUnity.core.macros;
using GenerationAttributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui {
  /// <summary>
  /// Allows to use same color in multiple assets throughout the project. We can make a color palette using these and it
  /// allows to easily change colors inside the project.
  /// </summary>
  [CreateAssetMenu, ExtractXMLDocIntoConst, TypeInfoBox(XML_DOC_ColorAsset)]
  public partial class ColorAsset : ScriptableObject {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [SerializeField, PublicAccessor] Color _color;
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
  }
}