using System;
using FPCSharpUnity.core.macros;
using GenerationAttributes;
using Sirenix.OdinInspector;

namespace FPCSharpUnity.unity.attributes {
  /// <summary>
  /// Adds an <see cref="InfoBoxAttribute"/> to child member in inspector. It's useful when we can't/don't want to add
  /// attribute to member field yourself. Also works with <see cref="EnumTypeAAttribute"/>!<br/>
  /// <code><![CDATA[
  /// [InfoBoxOnChildAttribute("field", "Text message contents")] public class Test {
  ///   public int field;
  /// }
  ///
  /// This will show InfoBox on top of 'field' in inspector.
  /// ]]></code>
  /// </summary>
  [Record] public partial class InfoBoxOnChildAttribute : Attribute {
    /// <summary> A member that we want to add InfoBox to. Has to be rendered in inspector. </summary>
    public readonly string fieldName;
    /// <summary> InfoBox contents. </summary>
    public readonly string message;
  }
}