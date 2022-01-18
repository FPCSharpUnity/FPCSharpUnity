using System;
using System.IO;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Net {
  /// <summary>
  /// https://docs.unity3d.com/ScriptReference/Networking.DownloadHandlerFile.html
  /// 
  /// for older unity versions.
  /// </summary>
  public class DownloadHandlerFile : DownloadHandlerScript {
    readonly FileStream file;

    /// <param name="file">File open for writing.</param>
    /// <param name="bufferSize">How many bytes should we cache in memory?</param>
    public DownloadHandlerFile(
      FileStream file, int bufferSize = 1024 * 512
    ) : base(new byte[bufferSize]) {
      this.file = file;
    }

    protected override bool ReceiveData(byte[] buffer, int dataLength) {
      // Weird. Unity docs say it's a ring buffer, but the data is always written to the
      // beginning of the buffer, at least on 5.6.5p1.
      // 
      // This implementation is tested by downloading a file with a browser, then with this
      // code and comparing sha256 hashes of those files.
      //
      // -- arturaz
      try {
        file.Write(buffer, 0, dataLength);
        return true;
      }
      catch (Exception e) {
        if (Log.d.isWarn())
          Log.d.warn(LogEntry.fromException($"Error while writing to file {file.Name}", e));
        F.doTry(() => file.Close());
        F.doTry(() => File.Delete(file.Name));
        return false;
      }
    }

    protected override void CompleteContent() => file.Dispose();
  }
}