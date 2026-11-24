using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class ParkingLotManager : MonoBehaviour
{
    [Header("---- Board Grid ----")]
    public int int_BoardWidth = 9;
    public int int_BoardHeight = 7;

    [Header("---- References ----")]
    public CarConfig carConfig;
    public Transform trans_ParkingSpacesParent;
    public List<CarControl> listCarControl_CarInParkingLot = new();
    public List<ParkingSpace> listParkingSpaces_All = new();
    public List<ParkingSpace> listParkingSpaces_current = new();
    public Transform trans_ParkingEntrance;
    [Tooltip("0: LeftDown, 1: LeftUp, 2: RightDown, 3: RightUp")]
    public List<Transform> listTrans_Points;

    //private int[,] occupancy;
    private Dictionary<CarControl, Vector2Int> carHeadCells = new();

    // ==== NEW: Lưu slot mục tiêu của từng xe ====
    private readonly Dictionary<CarControl, ParkingSpace> activeTargets = new();
    
    public IEnumerator HandleStart()
    {
        //InitializeBoard();
        RegisterCars();
        LoadParkingSpaces();
        yield return null;
    }

    void LoadParkingSpaces()
    {
        listParkingSpaces_All.Clear();
        listParkingSpaces_current.Clear();
        listParkingSpaces_All = trans_ParkingSpacesParent.GetComponentsInChildren<ParkingSpace>(true).ToList();
        for(int i = 1; i < 5; i++) // Bỏ qua VIP slot ở index 0
        {
            listParkingSpaces_current.Add(listParkingSpaces_All[i]);
        }
        foreach (var pk in listParkingSpaces_current)
        {
            pk.obj_Locked.SetActive(false);
            pk.obj_Unlocked.SetActive(true);
            pk.isOccupied = false;
            pk.isLocked = false;
        }
    }

    // void InitializeBoard()
    // {
    //     occupancy = new int[int_BoardWidth, int_BoardHeight];
    //     for (int x = 0; x < int_BoardWidth; x++)
    //         for (int y = 0; y < int_BoardHeight; y++)
    //             occupancy[x, y] = -1;
    // }

    void RegisterCars()
    {
        for (int i = 0; i < listCarControl_CarInParkingLot.Count; i++)
        {
            var car = listCarControl_CarInParkingLot[i];
            car.HandleStart();
            // Subscribe sự kiện tới đích
            car.OnArrivedParking += HandleArrivedParking;
        }
    }

    // --------------------------------------------------------------
    private void HandleArrivedParking(CarControl car)
    {
        // Xe đã chạm tới đúng vị trí slot (snap ở CarControl)
        if (activeTargets.TryGetValue(car, out var slot))
        {
            slot.Occupy();                  // Đánh dấu slot đã nhận xe
            activeTargets.Remove(car);
        }
    }
    
    /// <summary>
    /// Shuffle màu trong từng nhóm kích cỡ, chỉ cho các xe chưa rời bãi.
    /// Trả về số xe thực sự bị đổi màu.
    /// </summary>
    public int  ShuffleColorsSmart()
    {
        // Lấy các xe còn trong bãi
        var activeCars = listCarControl_CarInParkingLot.Where(c => c != null && !c.HasLeftLot && !c.isWaitingAtGate && c.gameObject.activeInHierarchy).ToList();
        if (activeCars.Count <= 1)
            return 0;

        int changedCount = 0;

        // Nhóm theo kích cỡ
        var groups = activeCars
            .GroupBy(c => c.carInfo.carSize)         // CarSize
            .ToList();

        foreach (var group in groups)
        {
            // Nhóm có < 2 xe thì bỏ qua (shuffle cũng như không)
            var cars = group.ToList();
            if (cars.Count <= 1) continue;

            // Lấy túi màu của nhóm
            var colors = cars.Select(c => c.carInfo.objectColor_car).ToList();

            // Xáo trộn Fisher–Yates
            FisherYatesShuffle(colors);

            // Gán lại – đếm số xe thật sự đổi màu để thống kê
            for (int i = 0; i < cars.Count; i++)
            {
                if (cars[i].carInfo.objectColor_car != colors[i])
                {
                    cars[i].SetColor(colors[i]);
                    changedCount++;
                }
            }
        }

        return changedCount;
    }

    // Fisher–Yates chuẩn
    private static void FisherYatesShuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        //listCarControl_CarInParkingLot = new List<CarControl>(FindObjectsOfType<CarControl>());
    }
}
