using System;
using GenerationAttributes;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger {
  [AttributeMacro(@"
{{
if !is_var_defined 'standalone'
  standalone = false
end
if !is_var_defined 'markAsImplicit'
  markAsImplicit = false
end
if !is_var_defined 'loggerName'
  loggerName = type.short_name
end

add_using 'FPCSharpUnity.core.log'
}}

static ILog __lazy_log;

{{ if markAsImplicit }}
  [GenerationAttributes.Implicit]
{{ end }}
static ILog log => __lazy_log ??= FPCSharpUnity.unity.Logger.Log.d.withScope(
  ""{{ loggerName }}"", standalone: {{ standalone }}
);
"), AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
  public class HasLoggerAttribute : Attribute {
    /// <summary>Sets the 'standalone' parameter of <see cref="ILogExts.withScope"/>. Default: false.</summary>
    public bool standalone;
    
    /// <summary>Attaches the <see cref="Implicit"/> attribute to the logger. Default: false.</summary>
    public bool markAsImplicit;

    /// <summary>
    /// The name of the logger. Defaults to the type short name.
    /// </summary>
    public string loggerName;
  }
}