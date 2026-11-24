using System;
using UnityEngine;
using AppLovinMax;
using UnityEngine.Serialization;

public class MaxAdsManager : MonoBehaviour
{
    public static MaxAdsManager Instance { get; private set; }
    
    [Header("-----------Options-----------")]
    public bool showBannerOnStart = true;
    public MaxSdkBase.BannerPosition bannerPosition = MaxSdkBase.BannerPosition.BottomCenter;
    
    [Header("-----------App Open Options-----------")]
    [Tooltip("Bật/tắt App Open Ads toàn cục")]
    public bool enableAppOpen = true;

    [Tooltip("Thử hiện App Open khi vừa init xong lần đầu (nếu đã ready)")]
    public bool showAppOpenOnLaunch = true;

    [Tooltip("Hiện App Open khi quay lại app từ nền (resume)")]
    public bool showAppOpenOnResume = true;

    [Tooltip("Delay trước khi show App Open lúc resume (ms) để tránh xung đột UI)")]
    public int appOpenResumeDelayMs = 150;
    
    [HideInInspector] public RectTransform trans_GameplayUIBottomRoot; // Gán UI gốc (chứa các nút Shuffle, Slot, ...)
    private float _bannerHeight = 0f;
    private Vector2 _originalUIPos;
    private bool _bannerAdjusted = false;
    [HideInInspector] public bool _isBannerVisible = false;


    // (Optional) bạn có thể gán event này từ game để trao thưởng cho người chơi
    public event Action OnUserEarnedReward;

    private bool _rewardGrantedThisAd;
    public bool IsShowingAd { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }
    
    private void OnDisable()
    {
        ResetUIPosition();
    }

    // ---------------- Initialization ----------------
    public void InitializeMaxSdk()
    {
        // (Tùy chọn) Nếu muốn test ads trên máy dev: bật test IDs trước khi init
        // MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[] { "YOUR_GAID_OR_IDFA" });
        
        // Bật log chi tiết để dễ soi logcat khi test
        MaxSdk.SetVerboseLogging(true);

        // Đánh dấu thiết bị hiện tại là test device (dùng GAID của bạn)
        //PC
        // MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[] {
        //     "96f1766c-ba96-4285-8583-7833e2acaf7b"
        // });
        //Laptop
        // MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[] {
        //     "dec37315-1e42-4298-96e7-fba1c41ed1b8"
        // });
        

        MaxSdk.SetSdkKey(Params.MAX_SDK_KEY);

        RegisterInterstitialCallbacks();
        RegisterRewardedCallbacks();
        RegisterBannerCallbacks();
        if (enableAppOpen) RegisterAppOpenCallbacks();

        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration config) =>
        {
            Debug.Log("[MAX] SDK Khởi tạo thành công");
            // Preload Interstitial/Rewarded ngay sau khi init xong
            LoadInterstitial();
            LoadRewarded();

            if (showBannerOnStart) CreateAndShowBanner();
            if (enableAppOpen) LoadAppOpen();
        };

        MaxSdk.InitializeSdk();
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // Khi quay lại app (resume)
        if (!pauseStatus && enableAppOpen && showAppOpenOnResume)
        {
            // Trì hoãn nhẹ để tránh show đè UI splash/loader của bạn
            //Invoke(nameof(ShowAppOpenIfReady), appOpenResumeDelayMs / 1000f);
        }
    }
    
    public void ResetOnUserEarnedReward()
    {
        OnUserEarnedReward = null;
    }

    // ---------------- Interstitial ----------------
    private void RegisterInterstitialCallbacks()
    {
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedToLoad;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (id, info) =>
        {
            Debug.Log("[MAX] Interstitial hiển thị");
            IsShowingAd = true;
            Time.timeScale = 0f; // tạm dừng game khi hiện interstitial
        };
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (id, error, info) =>
        {
            Debug.LogWarning("[MAX] Interstitial hiển thị lỗi: " + error.Code);
            LoadInterstitial(); // thử preload lại
        };
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (id, info) =>
        {
            Debug.Log("[MAX] Interstitial ẩn");
            LoadInterstitial(); // chuẩn bị cho lần kế tiếp
            IsShowingAd = false;
            Time.timeScale = 1f; // resume game khi đóng interstitial
        };
    }

    private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("[MAX] Interstitial load thành công");
    }

    private void OnInterstitialFailedToLoad(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogWarning("[MAX] Interstitial load thất bại: " + errorInfo.Code + " → retry");
        // Có thể thêm backoff ở đây; demo: load lại luôn
        Invoke(nameof(LoadInterstitial), 1.0f);
    }

    private void LoadInterstitial()
    {
        MaxSdk.LoadInterstitial(Params.INTERSTITIAL_AD_KEY);
    }

    public bool ShowInterstitial()
    {
        if (MaxSdk.IsInterstitialReady(Params.INTERSTITIAL_AD_KEY))
        {
            MaxSdk.ShowInterstitial(Params.INTERSTITIAL_AD_KEY);
            return true;
        }
        else
        {
            Debug.Log("[MAX] Interstitial chưa sẵn sàng, reloading…");
            LoadInterstitial();
            return false;
        }
    }

    // ---------------- Rewarded ----------------
    private void RegisterRewardedCallbacks()
    {
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (id, info) =>
            Debug.Log("[MAX] Rewarded load thành công");

        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (id, error) =>
        {
            Debug.LogWarning("[MAX] Rewarded load thất bại: " + error.Code + " → retry");
            Invoke(nameof(LoadRewarded), 1.0f);
        };

        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (id, info) =>
        {
            Debug.Log("[MAX] Rewarded hiển thị");
            _rewardGrantedThisAd = false;
            IsShowingAd = true;
        };

        // Callback xác nhận người chơi đã xem đủ để nhận thưởng
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (id, reward, info) =>
        {
            Debug.Log("[MAX] Rewarded earned: " + reward.Label + " x" + reward.Amount);
            _rewardGrantedThisAd = true;
            // Bạn có thể thưởng ngay tại đây, hoặc đợi khi ad đóng (an toàn hơn trong vài trường hợp)
            OnUserEarnedReward?.Invoke();
            //OnUserEarnedReward = null; // chỉ thưởng 1 lần cho 1 ad
        };

        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (id, error, info) =>
        {
            Debug.LogWarning("[MAX] Rewarded hiển thị thất bại: " + error.Code);
            LoadRewarded();
        };

        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (id, info) =>
        {
            Debug.Log("[MAX] Rewarded ẩn");
            // Nếu bạn muốn chỉ thưởng khi người chơi đóng ad sau khi đã earn:
            // if (_rewardGrantedThisAd) OnUserEarnedReward?.Invoke();
            LoadRewarded();
            IsShowingAd = false;
            Time.timeScale = 1f; // resume game khi đóng rewarded
        };
    }

    private void LoadRewarded()
    {
        MaxSdk.LoadRewardedAd(Params.REWARDED_AD_KEY);
    }

    public bool ShowRewarded()
    {
        if (MaxSdk.IsRewardedAdReady(Params.REWARDED_AD_KEY))
        {
            MaxSdk.ShowRewardedAd(Params.REWARDED_AD_KEY);
            return true;
        }
        else
        {
            Debug.Log("[MAX] Rewarded chưa sẵn sàng, reloading…");
            LoadRewarded();
            return false;
        }
    }

    // ---------------- Banner ----------------
    private void RegisterBannerCallbacks()
    {
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += (id, info) =>
        {
            Debug.Log("[MAX] Banner load thành công");

            if (trans_GameplayUIBottomRoot == null) return;

            // Ghi lại vị trí gốc nếu chưa có
            if (_originalUIPos == Vector2.zero)
                _originalUIPos = trans_GameplayUIBottomRoot.anchoredPosition;

            // Chiều cao banner
            _bannerHeight = MaxSdkUtils.GetAdaptiveBannerHeight();
#if UNITY_EDITOR
            if (_bannerHeight <= 0) _bannerHeight = 120f; // fallback trong Editor
#endif
            // Dời UI lên trên banner
            MoveUpUI();

            Debug.Log($"[MAX] Banner height: {_bannerHeight}, adjusted UI once.");
        };

        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (id, error) =>
        {
            Debug.LogWarning("[MAX] Banner load thất bại: " + error.Code);
            //_isBannerVisible = false;
        };

        MaxSdkCallbacks.Banner.OnAdClickedEvent += (id, info) =>
            Debug.Log("[MAX] Banner clicked");

        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (id, adInfo) =>
            Debug.Log("[MAX] Banner revenue event");
    }
    
    private void ResetUIPosition()
    {
        if (trans_GameplayUIBottomRoot != null && _bannerAdjusted)
        {
            trans_GameplayUIBottomRoot.anchoredPosition = _originalUIPos;
            _bannerAdjusted = false;
            _isBannerVisible = false;
            Debug.Log("[MAX] UI reset to original position");
        }
    }

    public void MoveUpUI()
    {
        // ⚠️ Nếu banner từng biến mất rồi load lại → reset lại vị trí trước khi đẩy lên
        if (_bannerAdjusted)
        {
            trans_GameplayUIBottomRoot.anchoredPosition = _originalUIPos;
            _bannerAdjusted = false;
        }

        // Đẩy UI lên (chỉ 1 lần cho mỗi lần banner hiển thị)
        trans_GameplayUIBottomRoot.anchoredPosition += new Vector2(0, _bannerHeight);
        _bannerAdjusted = true;
        _isBannerVisible = true;
    }
    
    public void CreateAndShowBanner()
    {
        // Chỉ tạo 1 lần
        MaxSdk.CreateBanner(Params.BANNER_AD_KEY, bannerPosition);

        // (Tùy chọn) set màu nền banner nếu muốn
        // MaxSdk.SetBannerBackgroundColor(bannerAdUnitId, Color.clear);

        MaxSdk.ShowBanner(Params.BANNER_AD_KEY);
    }

    public void HideBanner()
    {
        MaxSdk.HideBanner(Params.BANNER_AD_KEY);
        ResetUIPosition();
    }

    public void DestroyBanner()
    {
        MaxSdk.DestroyBanner(Params.BANNER_AD_KEY);
        ResetUIPosition();
    }
    
    // ---------------- App Open ----------------
    private void RegisterAppOpenCallbacks()
    {
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += (adUnitId, adInfo) =>
        {
            Debug.Log("[MAX] AppOpen load thành công");
        };

        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += (adUnitId, errorInfo) =>
        {
            Debug.LogWarning("[MAX] AppOpen load thất bại: " + errorInfo.Code + " → retry");
            // đơn giản: retry sau 1s (có thể dùng backoff)
            Invoke(nameof(LoadAppOpen), 1.0f);
        };

        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += (adUnitId, adInfo) =>
        {
            Debug.Log("[MAX] AppOpen hiển thị");
            IsShowingAd = true;
        };

        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) =>
        {
            Debug.LogWarning("[MAX] AppOpen hiển thị thất bại: " + errorInfo.Code);
            // Thử load lại
            LoadAppOpen();
        };

        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += (adUnitId, adInfo) =>
        {
            Debug.Log("[MAX] AppOpen ẩn");
            // Chuẩn bị cho lần kế tiếp
            LoadAppOpen();
            IsShowingAd = false;
        };
    }

    private void LoadAppOpen()
    {
        MaxSdk.LoadAppOpenAd(Params.APP_OPEN_AD_KEY);
    }

    /// <summary>
    /// Gọi chỗ nào bạn muốn (ví dụ sau init, hoặc khi resume app)
    /// </summary>
    public void ShowAppOpenIfReady()
    {
        if (!enableAppOpen) return;

        if (MaxSdk.IsAppOpenAdReady(Params.APP_OPEN_AD_KEY))
        {
            MaxSdk.ShowAppOpenAd(Params.APP_OPEN_AD_KEY);
        }
        else
        {
            Debug.Log("[MAX] AppOpen chưa sẵn sàng → Load");
            LoadAppOpen();
        }
    }
}
