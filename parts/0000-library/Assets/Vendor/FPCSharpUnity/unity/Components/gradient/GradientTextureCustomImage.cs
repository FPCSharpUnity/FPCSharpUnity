using FPCSharpUnity.unity.Components.ui;
using FPCSharpUnity.unity.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.gradient {
  public class GradientTextureCustomImage : GradientTextureBase {
#pragma warning disable 649
  // ReSharper disable FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local, NotNullMemberIsNotInitialized
    [SerializeField, NotNull] CustomImage image;
  // ReSharper restore FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local, NotNullMemberIsNotInitialized
#pragma warning restore 649

    protected override void setTexture(Texture2D texture) => image.sprite = texture.toSprite();
  }
}
