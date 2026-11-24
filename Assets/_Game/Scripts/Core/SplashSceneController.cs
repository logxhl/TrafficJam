using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SplashSceneController : MonoBehaviour
{
    [Header("-----------Config-----------")]
    public float float_MinShowTime = 5f;      // bảo đảm quay “một lúc”
    public string str_DefaultFirstScene = "Tutorial"; // scene mặc định khi mở game lần đầu

    [Header("-----------UI-----------")]
    public Slider slider_Loading;                           // kéo vào Inspector
    public float barFillSpeed = 3.5f;                      // tốc độ mượt của thanh

    float _bar; // giá trị nội bộ 0..1

    void Awake()
    {
        if (slider_Loading) slider_Loading.value = 0f;
        _bar = 0f;
        // đảm bảo không bị Time.timeScale ảnh hưởng
        Time.timeScale = 1f;
    }

    private IEnumerator Start()
    {
        // Chờ GameManager (sống cross-scene)
        while (GameManager.Instance == null)
            yield return null;

        // Đảm bảo loading hiện tối thiểu minShowTime
        float start = Time.time;

        // Quyết định scene đích:
        string targetScene;
        if (!SceneLoader.TryConsumeNext(out targetScene) || string.IsNullOrEmpty(targetScene))
        {
            // Khởi tạo data (lần đầu app mở).
            yield return StartCoroutine(GameManager.Instance.InitializeAsync());

            if (!DataManager.Ins.IsFirstLaunch)
                targetScene = "SampleScene"; // lần khởi động tiếp theo
            else
                targetScene = str_DefaultFirstScene; // lần khởi động đầu tiên
        }

        // Load scene đích async
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        // Đợi Unity tải xong (tới 0.9f là sẵn sàng)
        while (op.progress < 0.9f)
        {
            // map 0..0.9 -> 0..0.9 (giữ 0.1 để finish)
            float target = Mathf.Clamp01(op.progress / 0.9f) * 0.9f;
            UpdateBar(target);
            yield return null;
        }

        // 2) Bảo đảm min show time – đẩy bar lên ~0.95 trong khoảng thời gian còn lại
        float remain = float_MinShowTime - (Time.time - start);
        if (remain > 0f) yield return SmoothTo(0.95f, remain);

        // Lần khởi động đầu, nếu cần thì thử show (nếu đã có sẵn cache từ phiên trước)
        //if (MaxAdsManager.Instance.enableAppOpen && MaxAdsManager.Instance.showAppOpenOnLaunch) MaxAdsManager.Instance.ShowAppOpenIfReady();
        // Thử show AOA ở Splash nhưng chỉ đợi tối đa X giây
        // if (AdMobAppOpenManager.Instance != null && AdMobAppOpenManager.Instance.showOnLaunch && !DataManager.Ins.IsFirstLaunch)
        // {
        const float WAIT_READY_TIMEOUT = 2.0f; // tuỳ bạn 1.0–2.5s
        float t = 0f;

        // Vòng đợi ad trở nên sẵn sàng (không block vô hạn)
        // while (t < WAIT_READY_TIMEOUT)
        // {
        //     // Nếu show được thì chờ đóng rồi thoát vòng
        //     // if (AdMobAppOpenManager.Instance.IsAdAvailable())
        //     // {
        //         // AdMobAppOpenManager.Instance.TryShowAd();
        //         Debug.Log("[SplashScene] Show AOA ở Splash");
        //         // Chờ đến khi quảng cáo đóng
        //         while (AdMobAppOpenManager.Instance._isShowingAd) // <-- dùng property public (xem B)
        //             yield return null;
        //         break;
        //     }

        //     UpdateBar(0.98f);
        //     t += Time.unscaledDeltaTime; // dùng unscaled để không phụ thuộc Time.timeScale
        //     yield return null;
        // }
        // }


        // Xong xuôi mới cho phép qua scene
        op.allowSceneActivation = true;
        yield return SmoothTo(1.0f, 0.25f); // hiệu ứng hoàn tất nhẹ
    }

    // Cập nhật thanh theo target mượt mà mỗi frame
    void UpdateBar(float target01)
    {
        // giữ target trong [0..1] và không lùi thanh
        target01 = Mathf.Clamp01(target01);
        target01 = Mathf.Max(target01, _bar);

        _bar = Mathf.MoveTowards(_bar, target01, barFillSpeed * Time.unscaledDeltaTime);

        if (slider_Loading) slider_Loading.value = _bar;
    }

    IEnumerator SmoothTo(float target01, float maxSeconds = 0f)
    {
        float t = 0f;
        while (_bar < target01 - 0.001f)
        {
            UpdateBar(target01);
            if (maxSeconds > 0f)
            {
                t += Time.unscaledDeltaTime;
                if (t >= maxSeconds) break;
            }
            yield return null;
        }
    }
}