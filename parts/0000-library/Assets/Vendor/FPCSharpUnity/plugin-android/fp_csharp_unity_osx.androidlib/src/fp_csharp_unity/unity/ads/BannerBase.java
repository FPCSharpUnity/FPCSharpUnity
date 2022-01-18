package fp_csharp_unity.unity.ads;

import android.app.Activity;
import android.util.Log;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import fp_csharp_unity.unity.fns.Fn1;
import fp_csharp_unity.unity.util.Utils;

@SuppressWarnings("unused")
public abstract class BannerBase<Banner extends View> implements IStandardBanner {
    protected abstract String TAG();

    protected final Activity activity;

    protected Banner banner;

    protected BannerBase(
        Activity activity,
        final boolean isTopBanner, final BannerMode.Mode mode,
        final Fn1<Banner> createBanner
    ) {
        this(activity, isTopBanner, mode, createBanner, true);
    }

    protected BannerBase(
        Activity activity, final boolean isTopBanner, final BannerMode.Mode mode,
        final Fn1<Banner> createBanner, final boolean hideAfterCreation
    ) {
        this.activity = activity;

        Utils.runOnUiSafe("BannerBase constructor", new Runnable() {
            @Override
            public void run() {
                banner = createBanner.run();
                addToUI(mode, isTopBanner, hideAfterCreation);
            }
        });
    }

    protected void addToUI(BannerMode.Mode mode, boolean isTopBanner, boolean hideAfterCreation) {
        if (mode instanceof BannerMode.PercentileSize) {
            // Reason for these views in short - to be able to place, position, and scale
            // banner sizes based on percentage of the screen taken
            // https://inthecheesefactory.com/blog/know-percent-support-library/en
            BannerMode.PercentileSize _mode = (BannerMode.PercentileSize) mode;

            View spacerView = new View(activity);
            LinearLayout heightPlacement = new LinearLayout(activity);
            heightPlacement.setWeightSum(1);
            heightPlacement.setOrientation(LinearLayout.VERTICAL);
            float heightOffset = 1 - _mode.height;
            if (!isTopBanner) {
                heightPlacement.addView(spacerView, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, 0, heightOffset));
            }
            heightPlacement.addView(banner, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, 0, _mode.height));
            if (isTopBanner) {
                heightPlacement.addView(spacerView, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, 0, heightOffset));
            }

            float widthOffset = (1 - _mode.width) / 2;
            LinearLayout widthPlacement = new LinearLayout(activity);
            widthPlacement.setWeightSum(1);
            widthPlacement.setOrientation(LinearLayout.HORIZONTAL);
            View leftView = new View(activity), rightView = new View(activity);
            widthPlacement.addView(leftView, new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MATCH_PARENT, widthOffset));
            widthPlacement.addView(heightPlacement, new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MATCH_PARENT, _mode.width));
            widthPlacement.addView(rightView, new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MATCH_PARENT, widthOffset));

            FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT);
            activity.addContentView(widthPlacement, params);
        } else {
            int finalWidth, finalHeight;

            if (mode instanceof BannerMode.WrapContent) {
                finalWidth = FrameLayout.LayoutParams.MATCH_PARENT;
                finalHeight = FrameLayout.LayoutParams.WRAP_CONTENT;
            } else if (mode instanceof BannerMode.FixedSize) {
                BannerMode.FixedSize _mode = (BannerMode.FixedSize) mode;
                final float density = activity.getResources().getDisplayMetrics().density;
                finalWidth = applyDensity(_mode.width, density);
                finalHeight = applyDensity(_mode.height, density);
            } else {
                throw new RuntimeException("Unknown banner mode: " + mode);
            }

            int gravity = (isTopBanner ? Gravity.TOP : Gravity.BOTTOM) | Gravity.CENTER_HORIZONTAL;
            final FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(
                    finalWidth, finalHeight, gravity
            );
            Log.d(
                    TAG(),
                    "Adding banner to frame [width:" + finalWidth + " height:" + finalHeight + " gravity:" +
                            gravity + "]"
            );



            activity.addContentView(banner, params);
        }

        Log.d(TAG(), "Banner added to UI.");
        if (hideAfterCreation) setVisibilityRunsOnUiThread(false);
    }

    private final int applyDensity(final int value, final float density) {
        switch (value) {
            case FrameLayout.LayoutParams.MATCH_PARENT:
            case FrameLayout.LayoutParams.WRAP_CONTENT:
                return value;
            default:
                return (int) (value * density);
        }
    }

    @Override
    public final void load() {
        Utils.runOnUiSafe("Banner load", new Runnable() {
            @Override
            public void run() {
                loadRunsOnUiThread();
            }
        });
    }

    protected abstract void loadRunsOnUiThread();

    protected void setVisibilityRunsOnUiThread(boolean visible) {
        if (banner != null) {
            banner.setVisibility(visible ? View.VISIBLE : View.GONE);
            Log.d(TAG(), "Banner visible=" + visible);
        }
        else Log.d(TAG(), "Banner frame is null, can't set visibility");
    }

    @SuppressWarnings("unused")
    public void setVisibility(final boolean visible) {
        Utils.runOnUiSafe("Banner setVisibility", new Runnable() {
            @Override
            public void run() {
                setVisibilityRunsOnUiThread(visible);
            }
        });
    }

    @SuppressWarnings("unused")
    public final void destroy() {
        Utils.runOnUiSafe("Banner destroy", new Runnable() {
            @Override
            public void run() {
                destroyRunsOnUiThread();
            }
        });
    }

    protected void destroyRunsOnUiThread() {
        if (banner == null) return;
        ViewGroup parent = (ViewGroup) banner.getParent();
        if (parent != null) {
            beforeDestroyRunsOnUiThread();
            parent.removeView(banner);
            afterDestroyRunsOnUiThread();
        }
        banner = null;
    }

    protected void beforeDestroyRunsOnUiThread() {}
    protected void afterDestroyRunsOnUiThread() {}

    @Override public void onPause() {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                onPauseRunsOnUiThread();
            }
        });
    }
    @Override public void onResume() {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                onResumeRunsOnUiThread();
            }
        });
    }

    protected void onPauseRunsOnUiThread() {}
    protected void onResumeRunsOnUiThread() {}
}
