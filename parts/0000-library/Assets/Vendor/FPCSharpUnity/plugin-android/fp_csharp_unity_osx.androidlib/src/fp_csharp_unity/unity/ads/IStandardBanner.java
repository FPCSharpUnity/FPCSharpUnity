package fp_csharp_unity.unity.ads;

/**
 * Created by arturas on 2016-03-08.
 */
@SuppressWarnings("unused")
public interface IStandardBanner {
    void setVisibility(final boolean visible);
    void load();
    void destroy();
    void onPause();
    void onResume();
}
