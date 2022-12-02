using System;
using GenerationAttributes;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger {
  [AttributeMacro(@"
{{
if !is_var_defined 'standalone'
  standalone = false
end

add_using 'FPCSharpUnity.core.log'
}}

static ILog __lazy_log;
static ILog log => __lazy_log ??= FPCSharpUnity.unity.Logger.Log.d.withScope(
  ""{{ type.short_name }}"", standalone: {{ standalone }}
);
"), AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
  public class HasLoggerAttribute : Attribute {
    /// <summary>Sets the 'standalone' parameter of <see cref="ILogExts.withScope"/>. Default: false.</summary>
    public bool standalone;
  }
}