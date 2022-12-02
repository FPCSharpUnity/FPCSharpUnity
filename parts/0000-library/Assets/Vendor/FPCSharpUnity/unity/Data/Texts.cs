using System;
using FPCSharpUnity.unity.unity_serialization;
using FPCSharpUnity.unity.validations;
using GenerationAttributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace FPCSharpUnity.unity.Data {
  /// <summary>
  /// A wrapped <see cref="string"/> value. This class has a better drawer for editor inspector which supports multiline
  /// editing and text box auto-resize. This was created because a simple string in editor is drawn poorly and hard to
  /// edit.
  /// <br/>
  /// This is also useful to use together with <see cref="UnityOption"/> if you want for inspector to draw text field as
  /// text area. Because you can't add <see cref="TextAreaAttribute"/> on <see cref="UnityOption"/>. 
  /// </summary>
  [Serializable, InlineProperty] public partial class TextAreaString {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [
      SerializeField, NotNull, TextAreaAttribute(minLines: 2, maxLines: 30), HideLabel, PublicAccessor, 
      FormerlySerializedAs("_template")
    ] string _text = "";
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
    
    public static implicit operator string(TextAreaString v) => v._text; 
  }

  /// <summary>
  /// <inheritdoc cref="TextAreaString"/>. <br/>
  /// String cannot be empty here!
  /// </summary>
  [Serializable, InlineProperty] public partial class TextAreaStringNonEmpty {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [
      SerializeField, NotNull, NonEmpty, TextAreaAttribute(minLines: 2, maxLines: 30), HideLabel, PublicAccessor, 
      FormerlySerializedAs("_template")
    ] string _text = "";
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
    
    public static implicit operator string(TextAreaStringNonEmpty v) => v._text; 
  }
}