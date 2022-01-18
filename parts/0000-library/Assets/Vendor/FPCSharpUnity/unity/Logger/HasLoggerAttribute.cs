using System;
using GenerationAttributes;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger {
  [AttributeMacro(@"
{{
if !is_var_defined 'standalone'
  standalone = false
end
}}

static FPCSharpUnity.core.log.ILog __lazy_log;
static FPCSharpUnity.core.log.ILog log => 
  __lazy_log ??= FPCSharpUnity.core.log.ILogExts.withScope(
    FPCSharpUnity.unity.Logger.Log.d, ""{{ type.short_name }}"", standalone: {{ standalone }}
  );
"), AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
  public class HasLoggerAttribute : Attribute {
    /// <summary>Sets the 'standalone' parameter of <see cref="ILogExts.withScope"/>. Default: false.</summary>
    public bool standalone;
  }
}