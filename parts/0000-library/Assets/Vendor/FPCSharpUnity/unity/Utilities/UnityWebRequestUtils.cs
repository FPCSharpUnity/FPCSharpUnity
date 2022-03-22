using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Extensions {
  public static class UnityWebRequestUtils {
    public static UnityWebRequest get(string url, DownloadHandler downloadHandler) => 
      new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, downloadHandler, uploadHandler: null);
  }
}