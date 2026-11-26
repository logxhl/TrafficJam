using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    [Header("---- Refs ----")]
    public UIController uiController;
    public ParkingLotManager parkingLotManager;
    public GarageController garageController;
    public GuestManager guestManager;
    public PoolManager poolManager;
    public RuntimeLevelBuilder runtimeLevelBuilder;
    [Header("---- Game Control ----")]
    public bool isGameStarted = false;
    public int int_SortingNumberCarInParking = 0;
    public bool isUseVipSlot = false;
    public bool isLose = false;
    public bool isWin = false;
    [Header("---- Runtime ----")]
    public int int_TotalCoinsEarnedThisLevel = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        uiController.HandleAwake();
        guestManager.HandleAwake();
        poolManager.Init();
    }

    private IEnumerator Start()
    {
        uiController.HandleStart();

        yield return StartCoroutine(runtimeLevelBuilder.HandleStart());

        // Bước 1: chạy ParkingLotManager
        yield return StartCoroutine(parkingLotManager.HandleStart());

        // Bước 2: chạy GarageController
        if (runtimeLevelBuilder.SpawnedGarages.Count == 0 && garageController != null)
            yield return StartCoroutine(garageController.HandleStart());
        else
            foreach (var garage in runtimeLevelBuilder.SpawnedGarages)
                yield return StartCoroutine(garage.HandleStart());

        // Bước 3: chạy GuestManager
        yield return StartCoroutine(guestManager.HandleStart());
    }

    public void OnGameWin()
    {
        // TODO: logic thắng màn của bạn
        isWin = true;
        // GameManager.Instance.maxAdsManager.ShowInterstitial();
        uiController.ShowWinPanel();
    }

    public void OnGameLose(bool isOutOfSlots)
    {
        isLose = true;
        UnityEngine.Events.UnityAction once = null;
        if (isOutOfSlots)
        {
            // TODO: logic thua
            once = () =>
            {
                uiController.animationSequencerController_Notification.OnFinishedEvent
                    .RemoveListener(once); // gỡ sau lần đầu
                uiController.ShowLosePanel();
            };
        }
        else
        {
            // TODO: logic thua
            once = () =>
            {
                uiController.animationSequencerController_Notification.OnFinishedEvent
                    .RemoveListener(once); // gỡ sau lần đầu
                uiController.ShowRevivePanel();
            };
        }

        uiController.animationSequencerController_Notification.OnFinishedEvent.AddListener(once);
        uiController.txt_Notification.text = "out of slots";
        uiController.obj_NotificationPanel.SetActive(false);
        uiController.obj_NotificationPanel.SetActive(true);
    }

    public void ReplayLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        DataManager.Ins.NextLevel(runtimeLevelBuilder.level.Count);
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    public void HackCoins()
    {
        GameManager.Instance.dataManager.AddCoin(10000);
        uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
    }

    public void HackAbilities()
    {
        GameManager.Instance.dataManager.AddShuffle(1);
        GameManager.Instance.dataManager.AddSort(1);
        GameManager.Instance.dataManager.AddVipSlot(1);
        uiController.UpdateAbilityCountsDisplay();
    }

    // Abilities
    // ----------- Shuffle -----------
    public void OnUseShuffle()
    {
        int changed = parkingLotManager.ShuffleColorsSmart();
        if (changed > 0)
        {
            Debug.Log("✅ Shuffle thành công — vẫn còn nước đi hợp lệ!");
            // (tuỳ chọn) chơi hiệu ứng Shuffle
            uiController.HideShufflePanel();
            uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
            GameManager.Instance.dataManager.MinusShuffle();
            if (GameManager.Instance.dataManager.ShuffleCount > 0)
                uiController.text_ShuffleCount.text = GameManager.Instance.dataManager.ShuffleCount.ToString("N0");
            else
            {
                uiController.obj_QuantityShuffle.SetActive(false);
            }
        }
        else
        {
            GameManager.Instance.dataManager.AddCoin(Params.INT_SHUFFLEPRICE);
            uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
            Debug.Log("❌ Shuffle thất bại — không tìm được hoán vị hợp lệ.");
            // (tuỳ chọn) hiển thị cảnh báo hoặc hiệu ứng rung nút
        }
    }
    public void OnUseCoinShuffleButtonClicked()
    {
        if (GameManager.Instance.dataManager.TryMinusCoin(Params.INT_SHUFFLEPRICE))
        {
            uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
            OnUseShuffle();
        }
        else
        {
            Debug.Log("❌ Không đủ coin để Shuffle.");
            // (tuỳ chọn) hiển thị cảnh báo hoặc hiệu ứng rung nút
        }
    }
    public void OnWatchAdsShuffleButtonClicked()
    {
        // GameManager.Instance.maxAdsManager.OnUserEarnedReward += () =>
        // {
        OnUseShuffle();
        // };

        // GameManager.Instance.maxAdsManager.ShowRewarded();
    }

    // ----------- Sort -----------
    public void OnUseSort()
    {
        // Sort toàn hàng + gom đủ số lượng theo xe đang chờ & không bị block,
        // nếu thiếu thì spawn thêm đúng màu để đủ block
        guestManager.SortEnsureBlocksWaitingUnblockedFull();
        uiController.HideSortPanel();
        GameManager.Instance.dataManager.MinusSort();
        if (GameManager.Instance.dataManager.SortCount > 0)
            uiController.text_SortCount.text = GameManager.Instance.dataManager.SortCount.ToString("N0");
        else
        {
            uiController.obj_QuantitySort.SetActive(false);
        }
    }
    public void SortUseCoin()
    {
        if (GameManager.Instance.dataManager.TryMinusCoin(Params.INT_SORTPRICE))
        {
            uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
            OnUseSort();
        }
        else
        {
            Debug.Log("❌ Không đủ coin để Sort.");
            // (tuỳ chọn) hiển thị cảnh báo hoặc hiệu ứng rung nút
        }
    }

    public void SortWatchAds()
    {
        // GameManager.Instance.maxAdsManager.OnUserEarnedReward += () =>
        // {
        OnUseSort();
        // GameManager.Instance.maxAdsManager.ResetOnUserEarnedReward();
        // };

        // GameManager.Instance.maxAdsManager.ShowRewarded();
    }

    // ----------- Vip Slot -----------
    public void OnUseVipSlot()
    {
        uiController.HideVipSlotPanel();
        uiController.obj_UsingVipSlotPanel.SetActive(true);
        isUseVipSlot = true;
        parkingLotManager.listParkingSpaces_All[0].gameObject.SetActive(true);
        parkingLotManager.listParkingSpaces_current.Add(parkingLotManager.listParkingSpaces_All[0]);
        foreach (var car in parkingLotManager.listCarControl_CarInParkingLot)
        {
            car.spriteRenderer_CarBody.sortingLayerName = "Guest";
            car.spriteRenderer_CarBody.sortingOrder = 9001;
        }
        GameManager.Instance.dataManager.MinusVipSlot();
        if (GameManager.Instance.dataManager.VipSlotCount > 0)
            uiController.text_VipSlotCount.text = GameManager.Instance.dataManager.VipSlotCount.ToString("N0");
        else
        {
            uiController.obj_QuantityVipSlot.SetActive(false);
        }
    }
    public void VipSlotUseCoin()
    {
        if (GameManager.Instance.dataManager.TryMinusCoin(Params.INT_VIPSLOTPRICE))
        {
            uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
            OnUseVipSlot();
        }
        else
        {
            Debug.Log("❌ Không đủ coin để dùng Vip Slot.");
            // (tuỳ chọn) hiển thị cảnh báo hoặc hiệu ứng rung nút
        }
    }

    public void VipSlotWatchAds()
    {
        // GameManager.Instance.maxAdsManager.OnUserEarnedReward += () =>
        // {
        OnUseVipSlot();
        //     GameManager.Instance.maxAdsManager.ResetOnUserEarnedReward();
        // };

        // GameManager.Instance.maxAdsManager.ShowRewarded();
    }

    //----------- Extra Slot -----------
    public void UseCoinGetMoreParkingSpace()
    {
        if (DataManager.Ins.TryMinusCoin(Params.INT_EXTRASLOTPRICE))
        {
            foreach (var pk in parkingLotManager.listParkingSpaces_All)
            {
                if (!parkingLotManager.listParkingSpaces_current.Contains(pk) && !pk.isVipSlot)
                {
                    uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
                    guestManager.arrInt_wrongGuestColor = new int[guestManager.arrInt_wrongGuestColor.Length + 1];
                    uiController.HideSlotExtraPanel();
                    uiController.HideRevivePanel();
                    isLose = false;
                    pk.obj_Locked.SetActive(false);
                    pk.obj_Unlocked.SetActive(true);
                    pk.isOccupied = false;
                    pk.isLocked = false;
                    parkingLotManager.listParkingSpaces_current.Add(pk);
                    break;
                }
            }
        }
        else
        {
            Debug.Log("❌ Không đủ coin để mua chỗ đậu xe.");
            // (tuỳ chọn) hiển thị cảnh báo hoặc hiệu ứng rung nút
        }
    }

    public void WatchAdsGetMoreParkingSpace()
    {
        // GameManager.Instance.maxAdsManager.OnUserEarnedReward += () =>
        // {
        foreach (var pk in parkingLotManager.listParkingSpaces_All)
        {
            if (!parkingLotManager.listParkingSpaces_current.Contains(pk) && !pk.isVipSlot)
            {
                guestManager.arrInt_wrongGuestColor = new int[guestManager.arrInt_wrongGuestColor.Length + 1];
                uiController.HideSlotExtraPanel();
                uiController.HideRevivePanel();
                isLose = false;
                pk.obj_Locked.SetActive(false);
                pk.obj_Unlocked.SetActive(true);
                pk.isOccupied = false;
                pk.isLocked = false;
                parkingLotManager.listParkingSpaces_current.Add(pk);
                // GameManager.Instance.maxAdsManager.ResetOnUserEarnedReward();
                break;
            }
        }
        // };

        // GameManager.Instance.maxAdsManager.ShowRewarded();
    }
}
