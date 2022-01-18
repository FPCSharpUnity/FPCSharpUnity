#if UNITY_ANDROID
using System;
using FPCSharpUnity.unity.Android.Ads;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.video {
  public class AndroidVideoPlayer : IVideoPlayer {
    readonly MediaPlayerBinding binding;
    readonly Action onStartShow, onVideoComplete;

    public AndroidVideoPlayer(Action onStartShow, Action onVideoComplete) {
      binding = new MediaPlayerBinding();
      this.onStartShow = onStartShow;
      this.onVideoComplete = onVideoComplete;
    }

    public void playFromStreamingAssets(string fileName, Url clickUrl) {
      var listener = new VideoListener();
      if (Log.d.isDebug()) {
        listener.canceled += () => logDebug("canceled");
        listener.videoCompleted += () => logDebug("completed");
        listener.clicked += () => logDebug("clicked");
      }
      onStartShow();
      listener.videoCompleted += onVideoComplete;
      binding.showVideo(fileName, clickUrl.url, listener);
    }

    static void logDebug(string msg) {
      Log.d.debug($"{nameof(AndroidVideoPlayer)}|{msg}");
    }

    class MediaPlayerBinding : Binding {
      public MediaPlayerBinding()
        : base(new AndroidJavaObject("fp_csharp_unity.unity.video_player.VideoPlayerBridge")) { }

      public void showVideo(string fileName, string clickUrl, VideoListener listener)
        => java.CallStatic("playFromStreamingAssets", fileName, clickUrl, listener);
    }

    public class VideoListener : BaseAdListener {
      public VideoListener() : base("fp_csharp_unity.unity.video_player.VideoPlayerListener") { }
      public event Action canceled, videoCompleted, clicked;

      [UsedImplicitly] void onCancel() => invoke(() => canceled);
      [UsedImplicitly] void onVideoComplete() => invoke(() => videoCompleted);
      [UsedImplicitly] void onVideoClick() => invoke(() => clicked);
    }
  }
}
#endif