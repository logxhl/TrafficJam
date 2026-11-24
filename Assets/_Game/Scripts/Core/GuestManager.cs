using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GuestManager : MonoBehaviour
{
    [Header("----- Lineup -----")] public Transform trans_LineupRoot; // Parent chứa các điểm "1, 2, 3, ..."
    public float int_SpawnInterval = 0.1f;
    public int int_InitialSpawn = 21;

    [Header("----- Pool -----")] public GuestPool guestPool;

    [Header("----- Spawn Pathing ----- ")]
    public bool isWalkThroughAllSlotsOnSpawn = true; // ✅ đi lần lượt qua từng slot

    public float float_WaitAfterEachHop = 0.0f; // (tuỳ chọn) nghỉ một nhịp giữa mỗi chặng

    [Header("---- Runtime -----")] 
    public int[] arrInt_wrongGuestColor = new int[4];

    private int int_Guest;
    private List<Transform> listTrans_slots = new();

    public List<Guest> listGuests = new();

    // Kế hoạch spawn còn lại (FIFO) – chỉ dùng để bơm vào đuôi khi trống slot
    private Queue<ObjectColor> _spawnQueue = new();

    public void HandleAwake()
    {
        listTrans_slots = trans_LineupRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t != trans_LineupRoot)
            .ToList();
    }

    public IEnumerator HandleStart()
    {
        BuildSpawnPlanFromCars(); // điền _spawnQueue theo tổng capacity từng màu
        StartCoroutine(FallbackFillByRandomUpToSlots()); // ✅ CHỈ FILL ĐẾN GIỚI HẠN SLOTt
        arrInt_wrongGuestColor = new int[GameController.Instance.parkingLotManager.listParkingSpaces_current.Count];
        yield return null;
    }

    // Khi có người rời hàng → bơm đúng 1 khách (nếu còn trong kế hoạch)
    private void TopUpOne()
    {
        SpawnOneFromQueueIfAny();
    }

    private void SpawnOneFromQueueIfAny()
    {
        if (_spawnQueue.Count == 0) return; // hết kế hoạch → thôi
        if (listGuests.Count >= listTrans_slots.Count) return; // đủ slot → không bơm

        var color = _spawnQueue.Dequeue();
        SpawnOneAndEnqueue(color); // khách mới sẽ tự đi tới đuôi
    }

    private IEnumerator FallbackFillByRandomUpToSlots()
    {
        int maxVisible = listTrans_slots.Count;
        int target = Mathf.Min(int_InitialSpawn, maxVisible);

        for (int i = 0; i < target; i++)
        {
            SpawnOneFromQueueIfAny();
            if (int_SpawnInterval > 0) yield return new WaitForSeconds(int_SpawnInterval);
        }
    }

    public Guest SpawnOneAndEnqueue(ObjectColor? fixedColor = null)
    {
        // Chọn màu
        ObjectColor color;
        color = fixedColor.Value;

        // Lấy từ pool
        var g = guestPool.Get(color, transform);
        g.transform.position = transform.position; // điểm spawn tạm (ngoài hàng)
        g.gameObject.SetActive(true);
        g.OnFinishedBoard = OnGuestFinished;
        int random = Random.Range(0, 4);
        if (random == 0)
        {
            g.spriteRenderer_CoinSpecial.gameObject.SetActive(true);
            g.isHasSpecialCoin = true;
        }
        else
        {
            g.spriteRenderer_CoinSpecial.gameObject.SetActive(false);
            g.isHasSpecialCoin = false;
        }

        // Cho vào hàng (đuôi)
        listGuests.Add(g);

        int slotIdx = listGuests.Count - 1; // khách mới đứng ở đuôi hàng
        slotIdx = Mathf.Min(slotIdx, listTrans_slots.Count - 1); // clamp phòng thiếu slot

        // Đi qua từng slot đến vị trí của mình
        if (isWalkThroughAllSlotsOnSpawn && listTrans_slots.Count > 0)
            StartCoroutine(MoveGuestAlongSlots(g, slotIdx)); // ✅ đi lần lượt qua từng điểm
        //else if (slotIdx < listTrans_slots.Count) g.GoToPoint(listTrans_slots[slotIdx]); // đi thẳng (hành vi cũ)

        return g;
    }
    
    private void RepositionAll()
    {
        for (int i = 0; i < listGuests.Count; i++)
        {
            if (i < listTrans_slots.Count)
            {
                listGuests[i].GoToPoint(listTrans_slots[i]);
            }
            else
            {
                listGuests[i].GoToPoint(listTrans_slots[listTrans_slots.Count - 1]);
            }
        }
    }

    private void InstantRepositionAll()
    {
        for (int i = 0; i < listGuests.Count; i++)
            if (i < listTrans_slots.Count)
            {
                listGuests[i].TeleportToPoint(listTrans_slots[i]);
            }
    }

    private IEnumerator MoveGuestAlongSlots(Guest g, int targetSlotIdx)
    {
        // đảm bảo index hợp lệ
        targetSlotIdx = Mathf.Clamp(targetSlotIdx, 0, listTrans_slots.Count - 1);

        // đi từ slot 0 (đầu hàng) đến slot targetSlotIdx
        for (int i = listTrans_slots.Count - 1; i >= targetSlotIdx; i--)
        {
            var point = listTrans_slots[i];
            g.GoToPoint(point);

            // chờ khách tới điểm i
            while (Vector2.Distance(g.transform.position, point.position) > g.float_ArriveThreshold * 0.5f)
                yield return null;

            if (float_WaitAfterEachHop > 0f) yield return new WaitForSeconds(float_WaitAfterEachHop);

            if (i == 0) 
                GameController.Instance.isGameStarted = true;
        }
    }

    // ========== Boarding theo màu, TỪNG NGƯỜI MỘT ==========
    public void BoardPassengersSequentiallyByColor(CarControl car, Transform busDoor, int capacityLeft,
        ObjectColor color, float gap = 0.25f)
    {
        if (car.isBoarding || capacityLeft <= 0 || listGuests.Count == 0) return;
        StartCoroutine(BoardRoutine(car, busDoor, color, capacityLeft, gap));
    }

    private IEnumerator BoardRoutine(CarControl car, Transform busDoor, ObjectColor color, int capacityLeft, float gap)
    {
        if (GameController.Instance.isUseVipSlot && !car.isVipCar) yield break;
        
        car.isBoarding = true;

        int boarded = 0;
        while (boarded < capacityLeft)
        {
            boarded++;

            // tìm khách khớp màu gần ĐẦU HÀNG nhất
            int idx = FindFirstIndexMatchingColor(color);
            if (idx < 0)
            {
                int count = 0;
                int index = GameController.Instance.parkingLotManager.listParkingSpaces_current
                    .IndexOf(car.parkingSpace_current);
                if (index < arrInt_wrongGuestColor.Length)
                    arrInt_wrongGuestColor[index] = 1;
                for (int i = 0; i < arrInt_wrongGuestColor.Length; i++)
                {
                    if (arrInt_wrongGuestColor[i] == 1)
                    {
                        count++;
                    }
                }

                if (count >= arrInt_wrongGuestColor.Length)
                {
                    GameController.Instance.OnGameLose(false);
                    if (arrInt_wrongGuestColor.Length >=
                        GameController.Instance.parkingLotManager.listParkingSpaces_All.Count - 1)
                    {
                        GameController.Instance.OnGameLose(true);
                    }
                }
                else if(count == arrInt_wrongGuestColor.Length - 1)
                {
                    GameController.Instance.uiController.ShowWarningNotification();
                }

                car.isBoarding = false;
                break;
            }

            for (int i = 0; i < arrInt_wrongGuestColor.Length; i++)
            {
                if (arrInt_wrongGuestColor[i] == 1)
                {
                    arrInt_wrongGuestColor[i] = 0;
                }
            }

            var g = listGuests[idx];
            listGuests.RemoveAt(idx);

            // cho khách chạy lên cửa xe
            g.BoardTo(car);

            // dồn hàng
            RepositionAll();

            yield return new WaitForSeconds(gap);
        }
    }

    private int FindFirstIndexMatchingColor(ObjectColor color)
    {
        for (int i = 0; i < listGuests.Count; i++)
            if (listGuests[i].objectColor == color) return i;
            else return -1;
        return -1;
    }


    // callback khi khách biến mất (đã trả về pool)
    private void OnGuestFinished(Guest g, CarControl car)
    {
        if (car == null) return;
        
        if (g.isHasSpecialCoin)
        {
            GameController.Instance.uiController.ActiveSpecialCoinsCollectedUI(car.parkingSpace_current.trans_Canvas);
            g.isHasSpecialCoin = false;
            GameManager.Instance.dataManager.AddCoin(2);
            GameController.Instance.int_TotalCoinsEarnedThisLevel += 2;
            GameController.Instance.uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
        }
        else
        {
            GameManager.Instance.dataManager.AddCoin(1);
            GameController.Instance.int_TotalCoinsEarnedThisLevel += 1;
            GameController.Instance.uiController.UpdateCoinCountDisplay(GameManager.Instance.dataManager.Coin);
        }

        // Xe nhận 1 khách
        car.OnPassengerBoarded();

        // Bù đúng 1 khách mới từ kế hoạch (nếu còn) → luôn giữ tối đa = số slot
        TopUpOne();

        int_Guest -= 1;
        GameController.Instance.uiController.text_GuestCount.text = int_Guest.ToString();

        // Điều kiện thắng (tuỳ game)
        if (car.int_GuestCount >= car.Capacity)
        {
            car.isFulled = true;
            CarControl.RaiseCarDepartedFull(car.carInfo.objectColor_car, car.Capacity);
        }
    }

    private int GetCarCapacity(CarControl car)
    {
        // Nếu bạn đã có sẵn hàm/thuộc tính Capacity trong CarControl thì dùng thẳng:
        // return car.Capacity;

        // Còn nếu không, tạm suy ra từ size (điều chỉnh theo enum của bạn):
        switch (car.carInfo.carSize)
        {
            case CarSize.Big10Slots: return 10;
            case CarSize.Medium6Slots: return 6;
            default: return 4;
        }
    }

    private ObjectColor GetCarColor(CarControl car)
    {
        // Ưu tiên trường màu trên CarControl (tuỳ bạn đặt tên):
        // ví dụ: car.carColor (ObjectColor) — đổi nếu tên khác
        return car.carInfo.objectColor_car;
        // Nếu màu nằm ở nơi khác (vd car.carInfo.color), đổi trả về cho đúng.
    }

    private List<CarControl> GetActiveCars()
    {
        var cars = new List<CarControl>();
        if (GameController.Instance.parkingLotManager != null)
        {
            // Nếu bạn có danh sách xe trong ParkingLotManager thì dùng ở đây,
            if (GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot != null &&
                GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot.Count > 0)
                cars.AddRange(GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot);
        }

        if (cars.Count == 0)
        {
            // Fallback: quét scene
            cars.AddRange(FindObjectsOfType<CarControl>());
        }

        // Lọc xe hợp lệ
        cars = cars.Where(c => c != null && c.gameObject.activeInHierarchy || c.isWaitingAtGate).ToList();
        return cars;
    }

    private void BuildSpawnPlanFromCars()
    {
        _spawnQueue.Clear();

        var cars = GetActiveCars();
        if (cars == null || cars.Count == 0) return;

        // Gom tổng capacity theo màu
        var countByColor = new Dictionary<ObjectColor, int>();
        foreach (var car in cars)
        {
            var color = GetCarColor(car);
            int cap = Mathf.Max(0, GetCarCapacity(car));
            if (!countByColor.ContainsKey(color)) countByColor[color] = 0;
            countByColor[color] += cap;
            int_Guest += cap;
        }

        // ✅ Sắp xếp plan “có thể thắng” — spawn theo cụm màu tương ứng với xe
        var plan = new List<ObjectColor>();
        var planNotBlocked = new List<ObjectColor>();
        var planBlocked = new List<ObjectColor>();

        // Danh sách màu xe theo thứ tự thực tế (để đảm bảo spawn hợp với layout hiện tại)
        var carNotBlockedColors = cars
            .Where(c => !c.HasLeftLot && !c.isBlocked)
            //.Select(c => c.carInfo.objectColor_car)
            .ToList();
        
        var carBlockedColors = cars
            .Where(c => !c.HasLeftLot && c.isBlocked)
            //.Select(c => c.carInfo.objectColor_car)
            .ToList();

        // Lặp cho đến khi hết khách
        while (countByColor.Values.Sum() > 0)
        {
            foreach (var car in carNotBlockedColors)
            {
                if (!countByColor.ContainsKey(car.carInfo.objectColor_car)) continue;
                if (countByColor[car.carInfo.objectColor_car] <= 0) continue;

                // ✅ Mỗi vòng spawn cụm nhỏ (2–3 khách cùng màu)
                //int clusterSize = Mathf.Min(Random.Range(1, 6), countByColor[car.carInfo.objectColor_car]);
                for (int i = 0; i < car.Capacity; i++)
                    planNotBlocked.Add(car.carInfo.objectColor_car);

                countByColor[car.carInfo.objectColor_car] -= car.Capacity;
            }
            foreach (var car in carBlockedColors)
            {
                if (!countByColor.ContainsKey(car.carInfo.objectColor_car)) continue;
                if (countByColor[car.carInfo.objectColor_car] <= 0) continue;

                // ✅ Mỗi vòng spawn cụm nhỏ (2–3 khách cùng màu)
                //int clusterSize = Mathf.Min(Random.Range(1, 6), countByColor[car.carInfo.objectColor_car]);
                for (int i = 0; i < car.Capacity; i++)
                    planBlocked.Add(car.carInfo.objectColor_car);

                countByColor[car.carInfo.objectColor_car] -= car.Capacity;
            }
        }
        
        // Trộn ngẫu nhiên trong từng nhóm
        planNotBlocked = ConstrainedShuffle(planNotBlocked, frontSlots: 4, maxSameInRow: 3);
        planBlocked    = ConstrainedShuffle(planBlocked,    frontSlots: 4, maxSameInRow: 3);

        plan.Clear();
        plan.AddRange(planNotBlocked);
        plan.AddRange(planBlocked);
        
        // Loại bỏ chuỗi >5 màu khác nhau liên tiếp (auto fix)
        //FixImpossiblePattern(plan, 5);

        foreach (var c in plan)
            _spawnQueue.Enqueue(c);

        GameController.Instance.uiController.text_GuestCount.text = int_Guest.ToString();
    }

    // Fisher–Yates
    List<ObjectColor> ConstrainedShuffle(List<ObjectColor> input, int frontSlots = 4, int maxSameInRow = 3)
    {
        if (input == null || input.Count <= 1) return input;

        // Đếm số lượng còn lại mỗi màu
        var left = input.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
        var result = new List<ObjectColor>(input.Count);
        var rand = new System.Random();

        ObjectColor? last = null;
        int sameRun = 0;

        while (left.Values.Sum() > 0)
        {
            // 1) Ứng viên còn hàng
            var candidates = left.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();

            // 2) Giới hạn dây quá dài 1 màu
            if (last.HasValue && sameRun >= maxSameInRow)
            {
                candidates.Remove(last.Value);
                if (candidates.Count == 0) // hết lựa chọn -> nới lỏng
                    candidates = left.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            }

            // 3) Ràng buộc cửa sổ 4: không cho tạo mẫu A B C D
            int tail = Mathf.Min(frontSlots - 1, result.Count);
            if (tail > 0)
            {
                var recent = result.GetRange(result.Count - tail, tail);
                bool allDifferent = recent.Distinct().Count() == tail;

                if (allDifferent)
                {
                    // Nếu thêm 1 màu mới khác cả 3 cái sau lưng sẽ thành A B C D -> chặn
                    var allowed =
                        new HashSet<ObjectColor>(recent.Distinct()); // ưu tiên chọn 1 trong các màu vừa xuất hiện
                    var filtered = candidates.Where(c => allowed.Contains(c)).ToList();
                    if (filtered.Count > 0) candidates = filtered;
                }
            }

            // 4) Chọn theo trọng số (màu còn nhiều được ưu tiên)
            var weighted = new List<ObjectColor>();
            foreach (var c in candidates)
                for (int i = 0; i < left[c]; i++)
                    weighted.Add(c);

            // nếu vì ràng buộc mà rỗng, bỏ ràng buộc và chọn bất kỳ
            if (weighted.Count == 0)
            {
                foreach (var kv in left)
                    for (int i = 0; i < kv.Value; i++)
                        weighted.Add(kv.Key);
            }

            var pick = weighted[rand.Next(weighted.Count)];

            // 5) Ghi nhận
            result.Add(pick);
            left[pick]--;

            if (last.HasValue && pick.Equals(last.Value)) sameRun++;
            else
            {
                last = pick;
                sameRun = 1;
            }
        }

        return result;
    }


    /// <summary>
    /// Sort toàn bộ hàng theo thứ tự màu của các xe đang chờ parking và không bị block,
    /// với mỗi màu gom thành block liên tiếp có độ dài = tổng slot còn trống của tất cả xe màu đó.
    /// Nếu thiếu khách màu đó trong hàng, loại bớt màu đó khỏi _spawnQueue và spawn bù cho đủ.
    /// Phần dư (cùng màu hoặc màu khác) dồn về cuối theo thứ tự gốc.
    /// </summary>
    public void SortEnsureBlocksWaitingUnblockedFull()
    {
        if (listGuests == null || listGuests.Count == 0) return;

        // 1) Lấy xe thoả điều kiện: đang chờ đón khách + không bị block + chưa rời bãi
        var cars = GetActiveCars();
        var atParkingCars = cars
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .Where(c => c.HasLeftLot)
            .ToList();
        var unblockedCars = cars
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .Where(c => !c.HasLeftLot && !c.isBlocked)
            .ToList();
        if (unblockedCars.Count == 0 && atParkingCars.Count == 0) return;

        // 2) Thứ tự màu ưu tiên theo thứ tự xuất hiện của xe trong hàng chờ
        var colorsAtParking = new List<ObjectColor>();
        foreach (var car in atParkingCars)
        {
            GameController.Instance.int_SortingNumberCarInParking++;
            car.isInSort = true;
            var col = car.carInfo.objectColor_car;
            if (!colorsAtParking.Contains(col)) colorsAtParking.Add(col);
        }

        // 2.2. Sau đó là màu các xe unblocked (không lặp lại màu đã có ở trên)
        var colorsUnblocked = new List<ObjectColor>();
        foreach (var car in unblockedCars)
        {
            var col = car.carInfo.objectColor_car;
            if (!colorsAtParking.Contains(col) && !colorsUnblocked.Contains(col))
                colorsUnblocked.Add(col);
        }

        // --- 3) Tính NEED theo màu: split ra 2 đợt để ưu tiên đủ cho nhóm ở bãi trước ---
        var needAtParking = colorsAtParking.ToDictionary(c => c, _ => 0);
        foreach (var col in colorsAtParking)
        {
            foreach (var c in atParkingCars)
                if (c.carInfo.objectColor_car == col)
                    needAtParking[col] += Mathf.Max(0, c.Capacity - c.int_GuestCount);
        }

        var needUnblocked = colorsUnblocked.ToDictionary(c => c, _ => 0);
        foreach (var col in colorsUnblocked)
        {
            foreach (var c in unblockedCars)
                if (c.carInfo.objectColor_car == col)
                    needUnblocked[col] += Mathf.Max(0, c.Capacity - c.int_GuestCount);
        }

        // --- 4) Bucket hoá khách theo màu (FIFO, giữ thứ tự gốc) + set "used" để giữ ổn định tail ---
        var original = listGuests.ToList();
        var mapColorToQueue = new Dictionary<ObjectColor, Queue<Guest>>();
        var prioritizedAllColors = new HashSet<ObjectColor>(colorsAtParking.Concat(colorsUnblocked));

        foreach (var col in prioritizedAllColors)
            mapColorToQueue[col] = new Queue<Guest>();

        var othersQueue = new Queue<Guest>(); // khách màu không nằm trong 2 nhóm ưu tiên
        foreach (var g in original)
        {
            if (mapColorToQueue.TryGetValue(g.objectColor, out var q)) q.Enqueue(g);
            else othersQueue.Enqueue(g);
        }

        var result = new List<Guest>(original.Count + 32);
        var used = new HashSet<Guest>(); // track khách đã đưa lên result để build tail ổn định

        // --- 5) Giai đoạn 1: xếp ĐỦ cho các màu ở khu vực đón khách ---
        foreach (var col in colorsAtParking)
        {
            int need = needAtParking[col];
            if (need <= 0) continue;

            var q = mapColorToQueue[col];
            int have = q.Count;
            int take = Mathf.Min(have, need);

            // 5.1 lấy khách có sẵn
            for (int i = 0; i < take; i++)
            {
                var g = q.Dequeue();
                result.Add(g);
                used.Add(g);
            }

            // 5.2 spawn bù phần thiếu
            int missing = need - take;
            if (missing > 0)
            {
                RemoveFromQueueColor(col, missing); // loại khỏi kế hoạch spawn cũ
                for (int i = 0; i < missing; i++)
                {
                    var spawned = SpawnOneRawDetached(col); // khách mới, không nằm trong original
                    result.Add(spawned);
                    // nếu bạn cần cộng int_Guest/UI thì thêm ở đây
                    // int_Guest++;
                }
            }
        }

        // --- 6) Giai đoạn 2: xếp cho các màu unblocked (phần còn lại) ---
        foreach (var col in colorsUnblocked)
        {
            int need = needUnblocked[col];
            if (need <= 0) continue;

            var q = mapColorToQueue[col];
            int have = q.Count;
            int take = Mathf.Min(have, need);

            for (int i = 0; i < take; i++)
            {
                var g = q.Dequeue();
                result.Add(g);
                used.Add(g);
            }

            int missing = need - take;
            if (missing > 0)
            {
                RemoveFromQueueColor(col, missing);
                for (int i = 0; i < missing; i++)
                {
                    var spawned = SpawnOneRawDetached(col);
                    result.Add(spawned);
                    // int_Guest++;
                }
            }
        }

        // --- 7) Tail: dồn TẤT CẢ khách còn lại theo đúng thứ tự ban đầu (giữ ổn định toàn cục) ---
        foreach (var g in original)
        {
            if (!used.Contains(g)) result.Add(g);
        }

        // --- 8) Gắn lại list & reposition ---
        listGuests = result;

        int slotCount = (listTrans_slots != null) ? listTrans_slots.Count : 0;
        for (int i = 0; i < listGuests.Count; i++)
        {
            int index = i;
            if (i >= slotCount) index = slotCount - 1; // clamp phòng thiếu slot
            var guest = listGuests[i];
            var target = listTrans_slots[index];
            if (guest == null || target == null) continue;

            // Bạn có thể chuyển sang GoToPoint nếu muốn animation
            guest.TeleportToPoint(target);
        }
    }

    /// <summary>
    /// Bỏ tối đa 'maxCount' phần tử có màu 'color' khỏi _spawnQueue (giống ý tưởng EnsureFrontN).
    /// Trả về số lượng đã loại.
    /// </summary>
    private int RemoveFromQueueColor(ObjectColor color, int maxCount)
    {
        if (_spawnQueue == null || _spawnQueue.Count == 0 || maxCount <= 0) return 0;

        int removed = 0;
        var newQ = new Queue<ObjectColor>(_spawnQueue.Count);
        foreach (var c in _spawnQueue)
        {
            if (removed < maxCount && c.Equals(color))
            {
                removed++;
                continue;
            }
            newQ.Enqueue(c);
        }
        _spawnQueue = newQ;
        return removed;
    }

    /// <summary>
    /// Spawn 1 khách đúng màu nhưng KHÔNG chèn vào listGuests (để caller tự đặt vào vị trí mong muốn).
    /// </summary>
    private Guest SpawnOneRawDetached(ObjectColor fixedColor)
    {
        var g = guestPool.Get(fixedColor, transform);
        g.gameObject.SetActive(true);
        g.OnFinishedBoard = OnGuestFinished;
        g.transform.position = transform.position;
        int random = Random.Range(0, 4);
        if (random == 0)
        {
            g.spriteRenderer_CoinSpecial.gameObject.SetActive(true);
            g.isHasSpecialCoin = true;
        }
        else
        {
            g.spriteRenderer_CoinSpecial.gameObject.SetActive(false);
            g.isHasSpecialCoin = false;
        }
        return g;
    }
    
    /// <summary>
    /// VIP: Dồn đúng màu xe lên đầu hàng và đảm bảo đủ số lượng = sức chứa của xe (hoặc phần thiếu).
    /// Sau đó reposition lại toàn hàng để xe có thể nhận khách ngay.
    /// </summary>
    public void PrepareLineForCar(CarControl car)
    {
        var color = car.carInfo.objectColor_car;
        EnsureFrontN(color, car.Capacity);

        InstantRepositionAll();
    }

    /// <summary>
    /// Đảm bảo ĐẦU HÀNG có đúng 'targetFront' khách màu 'color'.
    /// - Kéo khách đúng màu sẵn có lên đầu (không spawn).
    /// - Nếu 'needToSpawn' > 0 thì spawn bù đúng số đó (nhưng không vượt quá thiếu thực tế).
    /// - spawnedExtra trả về số lượng thực sự đã spawn.
    /// </summary>
    private void EnsureFrontN(ObjectColor color, int targetFront)
    {
        targetFront = Mathf.Max(0, targetFront);

        // gom đúng màu và phần còn lại
        var sameColor = new List<Guest>();
        var others = new List<Guest>();
        foreach (var g in listGuests)
        {
            if (g.objectColor == color && sameColor.Count < targetFront)
                sameColor.Add(g);
            else
                others.Add(g);
        }

        // thiếu bao nhiêu để đủ 'targetFront' ở đầu?
        int missingForFront = Mathf.Max(0, targetFront - sameColor.Count);

        // Loại bỏ tối đa 'missingForFront' lần xuất hiện của 'color' khỏi _spawnQueue.
        Queue<ObjectColor> newQ = new Queue<ObjectColor>(_spawnQueue.Count);
        int removed = 0;
        foreach (var c in _spawnQueue)
        {
            if (removed < missingForFront && c.Equals(color))
            {
                removed++;
                continue;
            }

            newQ.Enqueue(c);
        }

        _spawnQueue = newQ;

        // chỉ spawn tối đa theo 'needToSpawn'
        //int spawnCount = Mathf.Min(missingForFront, Mathf.Max(0, needToSpawn));
        int spawnCount = missingForFront;
        for (int i = 0; i < spawnCount; i++)
        {
            sameColor.Insert(0, SpawnOneRaw(color));
        }

        // Ghép lại hàng
        listGuests.Clear();
        listGuests.AddRange(sameColor);
        listGuests.AddRange(others);
    }

    /// Spawn 1 khách đúng màu, chèn vào đầu (RepositionAll() sẽ sắp slot)
    private Guest SpawnOneRaw(ObjectColor fixedColor)
    {
        var g = guestPool.Get(fixedColor, transform);
        g.gameObject.SetActive(true);
        g.OnFinishedBoard = OnGuestFinished;
        g.transform.position = transform.position;
        int random = Random.Range(0, 4);
        if (random == 0)
        {
            g.spriteRenderer_CoinSpecial.gameObject.SetActive(true);
            g.isHasSpecialCoin = true;
        }
        else
        {
            g.spriteRenderer_CoinSpecial.gameObject.SetActive(false);
            g.isHasSpecialCoin = false;
        }
        listGuests.Insert(0, g);
        return g;
    }

}
