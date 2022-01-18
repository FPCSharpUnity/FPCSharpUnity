package fp_csharp_unity.unity.video_player;

public interface VideoPlayerListener {
  void onCancel();
  void onVideoComplete();
  void onVideoClick();
}
