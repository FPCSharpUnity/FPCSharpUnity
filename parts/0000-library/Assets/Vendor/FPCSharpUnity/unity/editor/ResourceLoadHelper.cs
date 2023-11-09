
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.editor; 

public partial class ResourceLoadHelper {
  
/// <summary>
/// In Editor, you can't load resources until this completes. If you load them sooner, then it will not load if
/// the asset file was changed between Unity launches.
/// </summary>
  public static Future<Unit> domainLoadedFuture =>
#if UNITY_EDITOR
    _editor_domainLoadedFuture;
#else
    Future.unit;
#endif
  
}
