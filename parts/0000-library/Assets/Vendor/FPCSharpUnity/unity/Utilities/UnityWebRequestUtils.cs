using GenerationAttributes;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Extensions {
  public static class UnityWebRequestUtils {
    /// <summary>Creates a GET request.</summary>
    /// <remarks>
    /// Identical to <see cref="UnityWebRequest.Get(string)"/>, but with ability to disable SSL certificate validation.
    /// </remarks>
    public static UnityWebRequest get(string url, bool validateCertificates = true) =>
      get(url, new DownloadHandlerBuffer(), validateCertificates);

    /// <inheritdoc cref="UnityWebRequestUtils.get(string,bool)"/>
    public static UnityWebRequest get(
      string url, DownloadHandler downloadHandler, bool validateCertificates = true
    ) {
      var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, downloadHandler, uploadHandler: null);
      if (!validateCertificates) {
        request.certificateHandler = dummyCertificateHandler;
      }
      return request;
    }

    [LazyProperty] static DummyCertificateHandler dummyCertificateHandler => new ();

    class DummyCertificateHandler : CertificateHandler {
      protected override bool ValidateCertificate(byte[] _) => true;
    }
  }
}
