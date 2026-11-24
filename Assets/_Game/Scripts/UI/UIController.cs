using BrunoMikoski.AnimationSequencer;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("---- Refs ----")]
    // win/lose/revive panels
    public GameObject obj_WinPanel;
    public GameObject obj_RevivePanel;
    public GameObject obj_LosePanel;
    // Notification
    public AnimationSequencerController animationSequencerController_Notification;
    public GameObject obj_NotificationPanel;
    public TextMeshProUGUI txt_Notification;

    // Text display
    public TextMeshProUGUI text_GuestCount;
    public TextMeshProUGUI text_CoinCount;
    public TextMeshProUGUI text_LevelIndex;
    public TextMeshProUGUI text_ShuffleCount;
    public TextMeshProUGUI text_ShuffleCountInPanel;
    public TextMeshProUGUI text_SortCount;
    public TextMeshProUGUI text_SortCountInPanel;
    public TextMeshProUGUI text_VipSlotCount;
    public TextMeshProUGUI text_VipSlotCountInPanel;
    public TextMeshProUGUI text_TotalEarnings;

    //In GamePlay Panel
    public GameObject obj_SettingsPanel;
    public GameObject obj_ReplayPanel;
    public GameObject obj_ShufflePanel;
    public GameObject obj_QuantityShuffle;
    public GameObject obj_BuyShufflePanel;
    public GameObject obj_SortPanel;
    public GameObject obj_BuySortPanel;
    public GameObject obj_QuantitySort;
    public GameObject obj_VipSlotPanel;
    public GameObject obj_BuyVipSlotPanel;
    public GameObject obj_QuantityVipSlot;
    public GameObject obj_UsingVipSlotPanel;
    public GameObject obj_SlotExtraPanel;
    public RectTransform trans_GameplayUIBottomRoot; // Gán UI gốc (chứa các nút Shuffle, Slot, ...)

    // Gameplay
    public GameObject obj_SpecialCoins;

    [Header("---- Runtime ----")]
    bool isReviveShown = false;

    public void HandleAwake()
    {
        // GameManager.Instance.maxAdsManager.trans_GameplayUIBottomRoot = trans_GameplayUIBottomRoot;
    }

    public void HandleStart()
    {
        // if(GameManager.Instance.maxAdsManager._isBannerVisible) GameManager.Instance.maxAdsManager.MoveUpUI();
        UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
        UpdateLevelTextDisplay(GameManager.Instance.dataManager.Level);
        UpdateAbilityCountsDisplay();
    }

    public void UpdateAbilityCountsDisplay()
    {
        text_ShuffleCount.text = GameManager.Instance.dataManager.ShuffleCount.ToString("N0");
        text_SortCount.text = GameManager.Instance.dataManager.SortCount.ToString("N0");
        text_VipSlotCount.text = GameManager.Instance.dataManager.VipSlotCount.ToString("N0");

        if (GameManager.Instance.dataManager.ShuffleCount <= 0)
            obj_QuantityShuffle.SetActive(false);
        else
        {
            obj_QuantityShuffle.SetActive(true);
        }
        if (GameManager.Instance.dataManager.SortCount <= 0)
            obj_QuantitySort.SetActive(false);
        else
        {
            obj_QuantitySort.SetActive(true);
        }
        if (GameManager.Instance.dataManager.VipSlotCount <= 0)
            obj_QuantityVipSlot.SetActive(false);
        else
        {
            obj_QuantityVipSlot.SetActive(true);
        }
    }
    public void UpdateCoinCountDisplay(int count)
    {
        text_CoinCount.text = count.ToString("N0");
    }
    public void UpdateLevelTextDisplay(int count)
    {
        text_LevelIndex.text = "Level " + (count + 1);
    }

    // win/lose/revive panels
    public void ShowWinPanel()
    {
        text_TotalEarnings.text = GameController.Instance.int_TotalCoinsEarnedThisLevel.ToString("N0");
        obj_WinPanel.SetActive(true);
    }
    public void ShowRevivePanel()
    {
        obj_NotificationPanel.SetActive(false);
        obj_RevivePanel.SetActive(true);
    }
    public void HideRevivePanel()
    {
        obj_RevivePanel.SetActive(false);
    }
    public void ShowLosePanel()
    {
        obj_RevivePanel.SetActive(false);
        obj_LosePanel.SetActive(true);
    }

    //In GamePlay Panel
    public void ShowSettingsPanel()
    {
        Time.timeScale = 0f;
        obj_SettingsPanel.SetActive(true);
    }
    public void HideSettingsPanel()
    {
        Time.timeScale = 1f;
        obj_SettingsPanel.SetActive(false);
    }
    public void ShowReplayPanel()
    {
        Time.timeScale = 0f;
        obj_ReplayPanel.SetActive(true);
    }

    public void ShowShufflePanel()
    {
        if (!GameController.Instance.isGameStarted || GameController.Instance.isUseVipSlot || GameController.Instance.int_SortingNumberCarInParking > 0) return;
        Time.timeScale = 0f;
        if (GameManager.Instance.dataManager.ShuffleCount > 0)
        {
            text_ShuffleCountInPanel.text = GameManager.Instance.dataManager.ShuffleCount.ToString("N0");
            obj_ShufflePanel.SetActive(true);
            return;
        }
        obj_BuyShufflePanel.SetActive(true);
    }
    public void HideShufflePanel()
    {
        Time.timeScale = 1f;
        obj_ShufflePanel.SetActive(false);
        obj_BuyShufflePanel.SetActive(false);
    }
    public void ShowSortPanel()
    {
        if (!GameController.Instance.isGameStarted || GameController.Instance.isUseVipSlot || GameController.Instance.int_SortingNumberCarInParking > 0) return;
        Time.timeScale = 0f;
        if (GameManager.Instance.dataManager.SortCount > 0)
        {
            text_SortCountInPanel.text = GameManager.Instance.dataManager.SortCount.ToString("N0");
            obj_SortPanel.SetActive(true);
            return;
        }
        obj_BuySortPanel.SetActive(true);
    }
    public void HideSortPanel()
    {
        Time.timeScale = 1f;
        obj_SortPanel.SetActive(false);
        obj_BuySortPanel.SetActive(false);
    }
    public void ShowVipSlotPanel()
    {
        if (!GameController.Instance.isGameStarted || GameController.Instance.isUseVipSlot || GameController.Instance.int_SortingNumberCarInParking > 0) return;
        Time.timeScale = 0f;
        if (GameManager.Instance.dataManager.VipSlotCount > 0)
        {
            text_VipSlotCountInPanel.text = GameManager.Instance.dataManager.VipSlotCount.ToString("N0");
            obj_VipSlotPanel.SetActive(true);
            return;
        }
        obj_BuyVipSlotPanel.SetActive(true);
    }
    public void HideVipSlotPanel()
    {
        Time.timeScale = 1f;
        obj_VipSlotPanel.SetActive(false);
        obj_BuyVipSlotPanel.SetActive(false);
    }
    public void HideUsingVipSlotPanel()
    {
        obj_UsingVipSlotPanel.SetActive(false);
        GameController.Instance.parkingLotManager.listParkingSpaces_All[0].spriteRenderer_ParkingVip.sortingLayerName = "Parking";
        GameController.Instance.parkingLotManager.listParkingSpaces_All[0].spriteRenderer_ParkingVip.sortingOrder = 0;
        GameController.Instance.parkingLotManager.listParkingSpaces_current.Remove(GameController.Instance
             .parkingLotManager.listParkingSpaces_All[0]);
        foreach (var car in GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot)
        {
            car.spriteRenderer_CarBody.sortingLayerName = "Cars";
            car.spriteRenderer_CarBody.sortingOrder = 1;
        }
    }
    public void ShowSlotExtraPanel()
    {
        Time.timeScale = 0f;
        obj_SlotExtraPanel.SetActive(true);
    }

    public void HideSlotExtraPanel()
    {
        Time.timeScale = 1f;
        obj_SlotExtraPanel.SetActive(false);
    }

    public void ShowWarningNotification()
    {
        txt_Notification.text = "only one spot left";
        obj_NotificationPanel.SetActive(true);
    }

    public void ActiveSpecialCoinsCollectedUI(Transform trans_ParkingCanvas = null, Vector2? anchoredStart = null)
    {
        // 1) Lấy object từ pool và gắn vào canvas đúng cách
        var go = PoolManager.Instance.GetObj(obj_SpecialCoins.gameObject);
        var parent = trans_ParkingCanvas != null ? trans_ParkingCanvas : this.transform;
        go.transform.SetParent(parent, worldPositionStays: false);
        go.SetActive(true);

        // 2) Cache component
        var rect = go.GetComponent<RectTransform>();
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();

        // 3) Reset trạng thái trước khi tween (phòng reuse từ pool)
        DOTween.Kill(go);
        DOTween.Kill(rect);
        rect.rotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.85f;
        rect.anchoredPosition = anchoredStart ?? Vector2.zero; // mặc định ở (0,0) của container
        cg.alpha = 0f;
        rect.gameObject.SetActive(true);

        // 4) Sequence hiệu ứng: pop-in nhẹ -> bay lên -> giữ 1 nhịp -> bay lên + mờ dần -> trả pool
        float up1 = 1.5f; // quãng bay lên lần 1 (pixel UI)
        float up2 = 0.5f; // quãng bay lên lần 2
        var seq = DOTween.Sequence().SetUpdate(true); // chạy được khi timeScale=0

        seq.Append(cg.DOFade(1f, 0.12f)); // hiện nhanh
        seq.Join(rect.DOScale(1f, 0.22f).From(0.6f).SetEase(Ease.OutBack));
        seq.Join(rect.DOAnchorPosY(rect.anchoredPosition.y + up1, 0.9f).SetEase(Ease.OutQuad));

        seq.AppendInterval(0.15f);
        seq.Append(rect.DOAnchorPosY(up2, 0.5f).SetRelative().SetEase(Ease.InSine));
        seq.Join(cg.DOFade(0f, 0.5f));

        seq.OnComplete(() =>
        {
            go.SetActive(false);
            // Nếu PoolManager có API trả về pool thì dùng nó:
            // PoolManager.Instance.ReleaseObj(go);
        });
    }

}
