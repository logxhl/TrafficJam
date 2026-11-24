using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

public class AdMobAppOpenManager : MonoBehaviour
{
    public static AdMobAppOpenManager Instance { get; private set; }

#if UNITY_ANDROID
    // AOA ID Android từ sheet của bạn
    private const string AD_UNIT_ID = "ca-app-pub-6409857233709298/8480480070";
#elif UNITY_IOS
    // AOA ID iOS từ sheet của bạn
    private const string AD_UNIT_ID = "ca-app-pub-6409857233709298/4389958103";
#else
    private const string AD_UNIT_ID = "unused";
#endif

    // App Open Ad hiện tại
    public AppOpenAd _appOpenAd;
    public bool _isShowingAd;
    public DateTime _adLoadTimeUtc;

    // AOA hết hạn sau 4 giờ (khuyến nghị của Google)
    private const int AD_EXPIRATION_HOURS = 4;

    // Tùy chọn
    public bool showOnLaunch = true;  // show khi mở app
    public bool showOnResume = true;  // show khi quay lại từ nền
    public float resumeDelaySeconds = 0.15f; // tránh đè UI/splash

    public void Init()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    public void Initialize()
    {
        // Bảo đảm event chạy trên main thread
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // Init SDK
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdMob AOA] MobileAds khởi tạo xong: " + initStatus.ToString());
            // Theo dõi app state (foreground/background)
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;

            // Preload lần đầu
            LoadAd();

            if (showOnLaunch)
            {
                // Thử show nếu đã sẵn sàng (có thể cache từ phiên trước)
                Invoke(nameof(TryShowAd), resumeDelaySeconds);
            }
        });
    }

    private void OnDestroy()
    {
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;

        if (_appOpenAd != null)
        {
            _appOpenAd.Destroy();
            _appOpenAd = null;
        }
    }

    // private void OnApplicationPause(bool pause)
    // {
    //     if (!pause && showOnResume)
    //     {
    //         // Resume → thử show
    //         Invoke(nameof(TryShowAd), resumeDelaySeconds);
    //     }
    // }

    private void OnAppStateChanged(AppState state)
    {
        if (state == AppState.Foreground && showOnResume && !MaxAdsManager.Instance.IsShowingAd)
        {
            Invoke(nameof(TryShowAd), resumeDelaySeconds);
        }
    }

    // --------- LOAD ----------
    public void LoadAd()
    {
        // Hủy bản cũ nếu còn
        if (_appOpenAd != null)
        {
            _appOpenAd.Destroy();
            _appOpenAd = null;
        }

        var request = new AdRequest();
        Debug.Log("[AdMob AOA] Loading... " + AD_UNIT_ID);

        // AOA cần chỉ định orientation; đặt Portrait (đổi nếu game landscape)
        AppOpenAd.Load(AD_UNIT_ID, request, (appOpenAd, loadError) =>
        {
            if (loadError != null)
            {
                Debug.LogWarning("[AdMob AOA] Load thất bại: " + loadError);
                // Retry
                Invoke(nameof(LoadAd), 1f);
                return;
            }

            _appOpenAd = appOpenAd;
            _adLoadTimeUtc = DateTime.UtcNow;
            RegisterCallbacks(_appOpenAd);
            Debug.Log("[AdMob AOA] load thành công");
        });
    }

    private void RegisterCallbacks(AppOpenAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdMob AOA] Opened");
            _isShowingAd = true;
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMob AOA] Closed");
            _isShowingAd = false;
            // Chuẩn bị lượt sau
            LoadAd();
        };

        ad.OnAdFullScreenContentFailed += (error) =>
        {
            Debug.LogWarning("[AdMob AOA] hiển thị thất bại: " + error);
            _isShowingAd = false;
            LoadAd();
        };

        // (Tùy chọn) theo dõi doanh thu
        ad.OnAdPaid += (adValue) =>
        {
            Debug.Log($"[AdMob AOA] Paid: {adValue.Value} {adValue.CurrencyCode} (precision {adValue.Precision})");
        };
    }

    public bool IsAdAvailable()
    {
        if (_appOpenAd == null)
        {
            Debug.Log("[AdMob AOA] No Ad Available");
        }
        else
        {
            Debug.Log("[AdMob AOA] Ad available, loaded at " + _adLoadTimeUtc.ToLocalTime());
        }
        return _appOpenAd != null &&
               (DateTime.UtcNow - _adLoadTimeUtc).TotalHours < AD_EXPIRATION_HOURS;
    }

    // --------- SHOW ----------
    public bool TryShowAd()
    {
        if (_isShowingAd)
        {
            Debug.Log("[AdMob AOA] Đang show rồi, bỏ qua");
            return false;
        }

        if (!IsAdAvailable())
        {
            Debug.Log("[AdMob AOA] Chưa có ad sẵn hoặc đã hết hạn, load lại");
            LoadAd();
            return false;
        }

        Debug.Log("[AdMob AOA] Show");
        _appOpenAd.Show();
        return true;
    }
}
