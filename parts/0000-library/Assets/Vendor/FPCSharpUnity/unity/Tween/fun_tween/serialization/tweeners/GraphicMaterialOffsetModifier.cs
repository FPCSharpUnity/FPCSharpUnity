using FPCSharpUnity.unity.Components.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [RequireComponent(typeof(Graphic)), ExecuteInEditMode]
  public class GraphicMaterialOffsetModifier : MonoBehaviour, IMaterialModifier, IMB_LateUpdate {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized

    // Serialized offset is used for 2 reasons:
    // 1. You can change offset in edit mode by changing this value.
    // 2. So it would undo the value to the default one after testing tween in edit mode.
    [SerializeField] Vector2 _offset;
    [SerializeField, InfoBox("_MainTex or _AlphaTexture")] string _textureNameInShader = "_MainTex";
    
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

    Material previousBaseMaterial, material;

    public Vector2 offset {
      get => _offset;
      set => _offset = value;
    }

    public void LateUpdate() {
      if (material) material.SetTextureOffset(_textureNameInShader, _offset);;
    }

    public Material GetModifiedMaterial(Material baseMaterial) {
      if (previousBaseMaterial != baseMaterial) {
        if (material) Destroy(material);

        var copy = new Material(baseMaterial);
        copy.SetTextureOffset(_textureNameInShader, _offset);
        material = copy;
        previousBaseMaterial = baseMaterial;
      }

      return material;
    }
  }
}