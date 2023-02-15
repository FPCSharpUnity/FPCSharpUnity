namespace FPCSharpUnity.unity.Pools; 

/// <summary>
/// Generic interface that can be used to pass custom profiling functions.
/// </summary>
public interface IProfiledScope {
  public void begin();
  public void end();
}