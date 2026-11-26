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
    /// Ưu tiên đưa màu của khách đang ở đầu hàng ra xe có thể đi được và không bị cone/xe khác chặn.
    /// Trả về số xe thực sự bị đổi màu.
    /// </summary>
    public int ShuffleColorsSmart()
    {
        // Lấy các xe còn trong bãi và chưa di chuyển/đỗ
        var activeCars = listCarControl_CarInParkingLot
            .Where(c => c != null && !c.HasLeftLot && !c.isWaitingAtGate &&
                        !c.isMoving && !c.isParking && !c.isParked && !c.isFulled &&
                        c.gameObject.activeInHierarchy)
            .ToList();
        if (activeCars.Count <= 1)
            return 0;

        ObjectColor? frontGuestColor = GetFrontGuestColor();
        int changedCount = 0;

        // Nhóm theo kích cỡ để bảo toàn tổng sức chứa theo màu
        var groups = activeCars
            .GroupBy(c => c.carInfo.carSize)
            .ToList();

        foreach (var group in groups)
        {
            var cars = group.ToList();
            if (cars.Count <= 1) continue;

            var originalColors = cars.Select(c => c.carInfo.objectColor_car).ToList();
            bool hasMultipleColors = originalColors.Distinct().Count() > 1;

            var movableIndices = new List<int>();
            for (int i = 0; i < cars.Count; i++)
            {
                if (IsCarClearToExit(cars[i]))
                    movableIndices.Add(i);
            }

            List<ObjectColor> colors = null;
            const int maxAttempts = 5;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                colors = new List<ObjectColor>(originalColors);
                FisherYatesShuffle(colors);

                if (frontGuestColor.HasValue && movableIndices.Count > 0)
                {
                    EnsureColorOnMovableCar(colors, movableIndices, frontGuestColor.Value);
                }

                if (!hasMultipleColors || HasAnyColorChanged(originalColors, colors))
                    break;
            }

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

    private static ObjectColor? GetFrontGuestColor()
    {
        var gm = GameController.Instance != null ? GameController.Instance.guestManager : null;
        if (gm != null && gm.listGuests != null && gm.listGuests.Count > 0 && gm.listGuests[0] != null)
            return gm.listGuests[0].objectColor;
        return null;
    }

    private static void EnsureColorOnMovableCar(List<ObjectColor> colors, List<int> movableIndices, ObjectColor targetColor)
    {
        foreach (int idx in movableIndices)
        {
            if (colors[idx] == targetColor)
                return;
        }

        var movableSet = new HashSet<int>(movableIndices);
        for (int j = 0; j < colors.Count; j++)
        {
            if (movableSet.Contains(j) || colors[j] != targetColor)
                continue;

            int targetIdx = movableIndices[Random.Range(0, movableIndices.Count)];
            (colors[targetIdx], colors[j]) = (colors[j], colors[targetIdx]);
            return;
        }
    }

    private static bool HasAnyColorChanged(List<ObjectColor> original, List<ObjectColor> shuffled)
    {
        for (int i = 0; i < original.Count; i++)
        {
            if (original[i] != shuffled[i])
                return true;
        }
        return false;
    }

    private static bool IsCarClearToExit(CarControl car)
    {
        if (car == null || car.isBlocked)
            return false;

        Vector2 dir = car.GetForwardDirection();
        if (dir == Vector2.zero)
            return false;

        Vector2 origin = car.transform.position;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, 30f);
        if (hits == null || hits.Length == 0)
            return true;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == null || car.OwnsCollider(hit.collider))
                continue;

            if (hit.collider.GetComponentInParent<BoundaryNode>() != null)
                return true;

            var cone = hit.collider.GetComponentInParent<ConeBlocker>();
            if (cone != null && cone.gameObject.activeInHierarchy && hit.collider.enabled)
                return false;

            var otherCar = hit.collider.GetComponentInParent<CarControl>();
            if (otherCar != null && otherCar != car && !otherCar.HasLeftLot && otherCar.gameObject.activeInHierarchy)
                return false;
        }

        return true;
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
