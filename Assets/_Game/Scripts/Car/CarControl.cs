using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CarControl : MonoBehaviour
{
    [Header("----- Base Data -----")]
    public CarInfo carInfo;

    [Header("----- Components -----")]
    public SpriteRenderer spriteRenderer_CarBody;
    public SpriteRenderer spriteRenderer_Arrow;
    [Tooltip("Các sprite con thể hiện hướng(0: Up, 1: UpLeft, 2: Down, 3: DownLeft, 4: Left, 5: Parking)")]
    public List<Sprite> listSprite_CarDirections; // các sprite con thể hiện hướng
    public SpriteColliderSync spriteColliderSync;
    public Transform trans_BusDoor;                   // điểm khách chạy tới (cửa xe)
    public List<SpriteRenderer> listSpriteRenderer_GuestsOnCar;    // danh sách khách trên xe (hiện tại chỉ dùng để ẩn hiện)
    
    [Header("----- Move Config -----")]
    public float float_MoveSpeed = 10f;
    public float float_GateRayCast = 2f;
    public LayerMask carLayer;

    [Header("----- Boarding Bounce -----")]
    [SerializeField] private Vector3 boardBouncePunch = new Vector3(0.025f, -0.04f, 0f);
    [SerializeField] private float boardBounceDuration = 0.16f;
    [SerializeField] private int boardBounceVibrato = 4;
    [SerializeField] private float boardBounceElasticity = 0.5f;

    // ==== NEW: Runtime Nav to parking ====
    [Header("----- Runtime Nav -----")]
    public bool HasLeftLot;          // <== Map theo project bạn
    public int int_GuestCount;
    public Vector2 v2_EntrancePos = new  Vector2(-1000f, -1000f);
    public bool isParking;           // đang có mục tiêu?
    public bool isMoving;
    public bool isParked;
    public bool isFulled;
    public bool isBoarding;
    public bool isBlocked;
    public bool isVipCar;
    public bool isInSort;
    
    // === Tiện ích ===
    public int Capacity => carInfo.carSize == CarSize.Big10Slots ? 10 :
        carInfo.carSize == CarSize.Medium6Slots ? 6 : 4;
    public ParkingSpace parkingSpace_current;
    private BoundaryNode boundaryNode_current;
    private Vector2 v2_base;
    private bool isCanControl = true;
    [HideInInspector] public bool isWaitingAtGate = false;
    [HideInInspector] public Sprite sprite_GarageSign;          // biểu tượng gara hiển thị trên mini map
    private Transform _root;
    private HashSet<Collider2D> _selfCols;          // tất cả collider thuộc chiếc xe này
    private Vector3 _baseBodyScale = Vector3.one;
    private Tween _boardBounceTween;
    private Transform BounceTarget => spriteRenderer_CarBody != null ? spriteRenderer_CarBody.transform : transform;

    // Sự kiện
    public event Action<CarControl> OnArrivedParking; // báo về ParkingLotManager
    public static event Action<ObjectColor, int> OnCarDepartedFull;
    public static void RaiseCarDepartedFull(ObjectColor color, int boarded)
        => OnCarDepartedFull?.Invoke(color, boarded);

    private void Awake()
    {
        v2_EntrancePos = new Vector2(-1000f, -1000f);
        isParking = false;
        _baseBodyScale = BounceTarget.localScale;
    }

    private void OnDisable()
    {
        _boardBounceTween?.Kill();
        _boardBounceTween = null;
    }

    public void HandleStart()
    {
        _root = transform;
        _selfCols = new HashSet<Collider2D>(_root.GetComponentsInChildren<Collider2D>(true));
        v2_EntrancePos = new Vector2(-1000f, -1000f); // khởi tạo xa khỏi map
        isParking = false;
        SetColor(carInfo.objectColor_car);
        v2_base = transform.position;
    }
    
    private void Update()
    {
        CheckBlockByRaycast();
        
        if(!isCanControl || GameController.Instance.isLose) return;
        
        if (isParked)
        {
            // Gọi boarding từng người một theo MÀU xe
            if (GameController.Instance.guestManager != null)
            {
                int capacityLeft = Capacity - int_GuestCount;
                if (capacityLeft > 0)
                {
                    // cho khách khớp màu lên từng người một
                    GameController.Instance.guestManager
                        .BoardPassengersSequentiallyByColor(this, trans_BusDoor != null ? trans_BusDoor : transform, capacityLeft, carInfo.objectColor_car, 0.25f);
                }
            }
        }
        
        if (isFulled)
        {
            if (isVipCar)
            {
                GameController.Instance.parkingLotManager.listParkingSpaces_All[0].gameObject.SetActive(false);
                GameController.Instance.parkingLotManager.listParkingSpaces_All[0].spriteRenderer_ParkingVip.sortingLayerName = "Guest";
                GameController.Instance.parkingLotManager.listParkingSpaces_All[0].spriteRenderer_ParkingVip.sortingOrder = 9000;
                GameController.Instance.isUseVipSlot = false;
                isVipCar = false;
            }
            transform.position = Vector2.MoveTowards(transform.position, v2_EntrancePos, float_MoveSpeed * Time.deltaTime);
        }
        
        if (((isFulled && isParked) || (isMoving && !isParked && parkingSpace_current != null)) &&
            Vector2.Distance(transform.position, v2_EntrancePos) < 0.3f)
        {
            if (isParked)
            {
                if (parkingSpace_current != null)
                {
                    parkingSpace_current.isOccupied = false;
                    parkingSpace_current = null;
                }
                carInfo.directions_Car = Directions.Right;
                isMoving = true;
                isFulled = false;
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer_CarBody.sprite = listSprite_CarDirections[5]; // sprite parking
                spriteColliderSync.ApplyCurrentSpriteShape();
                isMoving = false;
                isParking = true;
            }
        }

        // Nếu có điểm đích → kiểm tra đã tới chưa
        if (isParking)
        {
            Park();
        }
        
        if (!isMoving) return;
        // Luôn đảm bảo hình xoay khớp hướng enum (hướng sẽ đổi khi đụng BoundaryNode)
        SetupVisuals();

        // Di chuyển thẳng theo hướng
        switch (carInfo.directions_Car)
        {
            case Directions.Up:
                transform.Translate(Vector2.up * Time.deltaTime * float_MoveSpeed, Space.World);
                break;

            case Directions.UpLeft:
                transform.Translate(new Vector2(-1f, 1f).normalized * Time.deltaTime * float_MoveSpeed, Space.World);
                break;

            case Directions.UpRight:
                transform.Translate(new Vector2(1f, 1f).normalized * Time.deltaTime * float_MoveSpeed, Space.World);
                break;

            case Directions.Right:
                transform.Translate(Vector2.right * Time.deltaTime * float_MoveSpeed, Space.World);
                break;

            case Directions.Down:
                transform.Translate(Vector2.down * Time.deltaTime * float_MoveSpeed, Space.World);
                break;

            case Directions.Left:
                transform.Translate(Vector2.left * Time.deltaTime * float_MoveSpeed, Space.World);
                break;

            case Directions.DownLeft:
                transform.Translate(new Vector2(-1f, -1f).normalized * Time.deltaTime * float_MoveSpeed, Space.World);
                break;

            case Directions.DownRight:
                transform.Translate(new Vector2(1f, -1f).normalized * Time.deltaTime * float_MoveSpeed, Space.World);
                break;
        }
    }

    private void LateUpdate()
    {
        spriteColliderSync.HandleLateUpdate();
    }
    public void SetActiveControl(bool value)
    {
        isCanControl = value;
        // nếu bạn có AI/movement riêng thì enable/disable tại đây
        // vd: navAgent.enabled = value; hoặc bật/tắt update trong Update()
    }

    public void SetWaitingAtGate(bool value)
    {
        isWaitingAtGate = value;
        // có thể bật icon mũi tên, đèn garage, v.v…
    }

    // Được gọi khi người chơi bấm vào xe đang đứng chờ
    public void OnLeaveGate()
    {
        isMoving = true;
        spriteRenderer_Arrow.gameObject.SetActive(false);
    }
    
    // Gọi hàm này để đổi màu + cập nhật sprite/material/UI liên quan
    public void SetColor(ObjectColor c)
    {
        carInfo.objectColor_car = c;
        // TODO: cập nhật sprite/material/outline... theo color c
        // ví dụ: spriteRenderer.sprite = skinByColor[c]; ...
        foreach (var carConfig in GameController.Instance.parkingLotManager.carConfig.carInfoConfigs)
        {
            if (carConfig.carColor == c && carConfig.carSize == carInfo.carSize)
            {
                listSprite_CarDirections = carConfig.listSprite_CarDirections;
                SetupVisuals();
                foreach (var guest in listSpriteRenderer_GuestsOnCar)
                {
                    guest.sprite = carConfig.sprite_GuestInCar;
                    sprite_GarageSign = carConfig.sprite_GarageSign;
                }
                return;
            }
        }
    }

    public void SetupVisuals()
    {
        if (carInfo == null) { Debug.LogWarning($"🚗 {name} chưa có CarInfo!"); return; }

        //Xoay theo hướng (chỉ dùng trục Z vì game 2D)
        switch (carInfo.directions_Car)
        {
            case Directions.Up:
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, 0);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[0];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
            case Directions.UpLeft:
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, 50f);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[1];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
            case Directions.UpRight:
            {
                transform.rotation = Quaternion.Euler(0, -180f, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, -50f);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[1];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
            case Directions.DownRight:
            {
                transform.rotation = Quaternion.Euler(0, -180f, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, -130f);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[3];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
            case Directions.DownLeft:
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, 130f);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[3];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
            case Directions.Right:
            {
                transform.rotation = Quaternion.Euler(0, -180f, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, -90f);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[4];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
            case Directions.Down:
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, 180f);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[2];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
            case Directions.Left:
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer_Arrow.transform.rotation = Quaternion.Euler(0, 0, 90f);
                if (listSprite_CarDirections.Count > 0)
                {
                    spriteRenderer_CarBody.sprite = listSprite_CarDirections[4];
                    spriteColliderSync.ApplyCurrentSpriteShape();
                }

                break;
            }
        }
    }

    private void OnMouseUpAsButton()
    {
        if (!isCanControl || GameController.Instance.isLose) return; // ✅ thêm dòng này để xe đang chờ ở cửa không nhận click "đi park"
        if (isMoving || isParking || isParked || isFulled || !GameController.Instance.isGameStarted || Time.timeScale <= 0f) return;
        if (GameController.Instance.isUseVipSlot)
        {
            var vipSpace = GameController.Instance.parkingLotManager.listParkingSpaces_All[0];
            if (vipSpace == null || vipSpace.isOccupied) return;

            HasLeftLot = true;
            isVipCar = true;
            // ✅ CHUẨN BỊ HÀNG KHÁCH CHO VIP (dồn đúng màu + spawn bù)
            var gm = GameController.Instance.guestManager;
            if (gm != null)
            {
                gm.PrepareLineForCar(this); // dồn đúng màu lên đầu + đủ số lượng
            }
            
            parkingSpace_current = vipSpace;
            parkingSpace_current.isOccupied = true;

            // ✅ LẤY ENTRANCE CỦA SLOT VIP cho bước rời bãi
            v2_EntrancePos = parkingSpace_current.transform.GetChild(0).position;

            spriteRenderer_Arrow.sprite = null;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            spriteRenderer_CarBody.sprite = listSprite_CarDirections[5]; // sprite parking
            spriteColliderSync.ApplyCurrentSpriteShape();

            DOTween.Kill(transform);
            transform.DOMove(parkingSpace_current.trans_anchor.position, 1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.position = parkingSpace_current.trans_anchor.position;
                    isParking = false;
                    isMoving = false;
                    isParked = true;   // ⚠️ đặt sau khi PrepareLineForCar để Update() bắt đầu boarding ngay

                    OnArrivedParking?.Invoke(this);
                    GameController.Instance.uiController.HideUsingVipSlotPanel();
                });
            return;
        }
        parkingSpace_current = FindNextAvailableParkingSpace();
        if(parkingSpace_current == null)
        {
            // Không có chỗ đỗ trống
            Debug.Log("🚗 Không có chỗ đỗ trống!");
            return;
        }
        isMoving = true;
        SetWaitingAtGate(false);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            gameObject.SetActive(false);
            // Nếu hết khách → thắng
            if (GameController.Instance.guestManager.listGuests.Count == 0 &&
                !GameController.Instance.isWin)
            {
                GameController.Instance.OnGameWin();
            }
        }
    }

    // Đổi hướng khi chạm node góc: BoundaryNode quyết định hướng mới (rules[0].outgoing)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Hoặc nếu collider nằm trong cùng root (ví dụ xe có collider con)
        // if (other.transform.root == transform.root)
        //     return;
        
        if(isParked || GameController.Instance.isUseVipSlot || isWaitingAtGate) return;
        // Bỏ qua collider của chính mình
        if (other.gameObject == gameObject)
            return;
        
        // 1. Nếu là BoundaryNode thì xử lý rẽ như cũ
        var node = other.GetComponent<BoundaryNode>();
        if (node != null && node.rules != null && node.rules.Count > 0)
        {
            int defaultLayer = LayerMask.NameToLayer("Default");
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = defaultLayer;
            }
            HasLeftLot = true;
            spriteRenderer_Arrow.sprite = null;
            if(node.kind == BoundaryNode.NodeKind.Entrance)
            {
                if (parkingSpace_current != null)
                {
                    if (v2_EntrancePos.x < transform.position.x)
                    {
                        if(Vector2.Distance(transform.position, GameController.Instance.parkingLotManager.listTrans_Points[3].position) < 1.5f)
                            transform.position = GameController.Instance.parkingLotManager.listTrans_Points[3].position;
                        else
                        {
                            transform.position = new Vector2(transform.position.x, GameController.Instance.parkingLotManager.listTrans_Points[3].position.y);
                        }
                        carInfo.directions_Car = Directions.Left;
                    }
                    else if (v2_EntrancePos.x > transform.position.x)
                    {
                        if(Vector2.Distance(transform.position, GameController.Instance.parkingLotManager.listTrans_Points[1].position) < 1.5f)
                            transform.position = GameController.Instance.parkingLotManager.listTrans_Points[1].position;
                        else
                        {
                            transform.position = new Vector2(transform.position.x, GameController.Instance.parkingLotManager.listTrans_Points[3].position.y);
                        }
                        carInfo.directions_Car = Directions.Right;
                    }
                }
                boundaryNode_current = node;
                return;
            }

            if (node.kind == BoundaryNode.NodeKind.LeftDown)
            {
                if (Vector2.Distance(transform.position,
                        GameController.Instance.parkingLotManager.listTrans_Points[0].position) < 1.5f)
                {
                    transform.position = GameController.Instance.parkingLotManager.listTrans_Points[0].position;
                    carInfo.directions_Car = Directions.Up;
                    return;
                }
                transform.position =
                    new Vector2(GameController.Instance.parkingLotManager.listTrans_Points[0].position.x,
                        transform.position.y);
                
                if (Vector2.Distance(transform.position,
                        GameController.Instance.parkingLotManager.listTrans_Points[1].position) < 1.5f)
                {
                    transform.position = GameController.Instance.parkingLotManager.listTrans_Points[1].position;
                    carInfo.directions_Car = Directions.Right;
                    return;
                }
            }
            else if (node.kind == BoundaryNode.NodeKind.RightDown)
            {
                if (Vector2.Distance(transform.position,
                        GameController.Instance.parkingLotManager.listTrans_Points[2].position) < 1.5f)
                {
                    transform.position = GameController.Instance.parkingLotManager.listTrans_Points[2].position;
                    carInfo.directions_Car = Directions.Up;
                    return;
                }
                transform.position =
                    new Vector2(GameController.Instance.parkingLotManager.listTrans_Points[2].position.x,
                        transform.position.y);
                
                if (Vector2.Distance(transform.position,
                        GameController.Instance.parkingLotManager.listTrans_Points[3].position) < 1.5f)
                {
                    transform.position = GameController.Instance.parkingLotManager.listTrans_Points[3].position;
                    carInfo.directions_Car = Directions.Left;
                    return;
                }
            }
            else if (node.kind == BoundaryNode.NodeKind.Wall)
            {
                transform.position = new Vector2(transform.position.x, GameController.Instance.parkingLotManager.listTrans_Points[0].position.y);
            }
            boundaryNode_current = node;
            carInfo.directions_Car = node.rules[0].outgoing;
        }
        
        if (isBlocked && !HasLeftLot)
        {
            isMoving = false;
            // Cập nhật trạng thái bãi xe
            if (parkingSpace_current != null)
            {
                parkingSpace_current.isOccupied = false;
                parkingSpace_current = null;
            }
            v2_EntrancePos = new Vector2(-1000f, -1000f);
            // Hủy tween cũ nếu đang có
            DOTween.Kill(transform);

            // Di chuyển mượt về vị trí ban đầu
            transform.DOMove(v2_base, 0.2f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    spriteRenderer_Arrow.gameObject.SetActive(true);
                });
        }
    }
    
    // Tìm ParkingSpace trống (theo thứ tự trái sang phải)
    private ParkingSpace FindNextAvailableParkingSpace()
    {
        // Lấy tất cả parkingSpaces từ ParkingLotManager
        var lot = GameController.Instance.parkingLotManager;
        if (lot == null)
        {
            isMoving = false;
            return null;
        }

        foreach (var space in lot.listParkingSpaces_current)
        {
            if (space != null && !space.isOccupied && !space.isLocked && !space.isVipSlot) // tùy thuộc vào logic slot của bạn
            {
                v2_EntrancePos = space.transform.GetChild(0).position;
                parkingSpace_current = space;
                parkingSpace_current.isOccupied = true; // Đánh dấu tạm thời để tránh xe khác chọn trùng
                spriteRenderer_Arrow.gameObject.SetActive(false);
                return space;
            }
        }
        return null;
    }
    
    // Di chuyển tới chỗ đỗ
    private void Park()
    {
        if (parkingSpace_current == null || parkingSpace_current.trans_anchor == null)
        {
            isParking = false;
            return;
        }

        if (Vector2.Distance(transform.position, parkingSpace_current.trans_anchor.position) < 0.1f)
        {
            // Dừng tại chỗ đỗ
            transform.position = parkingSpace_current.trans_anchor.position;
            isParking = false;
            isMoving = false;
            isParked = true;
            OnArrivedParking?.Invoke(this);
            return;
        }
        transform.position = Vector2.MoveTowards(transform.position, parkingSpace_current.trans_anchor.position, float_MoveSpeed * Time.deltaTime);
    }

    public void OnPassengerBoarded()
    {
        PlayBoardBounce();
        listSpriteRenderer_GuestsOnCar[int_GuestCount].gameObject.SetActive(true);
        int_GuestCount++;
        if (int_GuestCount >= Capacity && !isFulled)
        {
            //isFulled = true;
            BeginExit(); // tách riêng logic rời bãi cho gọn
            foreach (var g in listSpriteRenderer_GuestsOnCar)
            {
                g.gameObject.SetActive(false);
            }
        }
    }

    private void PlayBoardBounce()
    {
        Transform target = BounceTarget;
        if (target == null) return;

        _boardBounceTween?.Kill();
        target.localScale = _baseBodyScale;
        _boardBounceTween = target
            .DOPunchScale(boardBouncePunch, boardBounceDuration, boardBounceVibrato, boardBounceElasticity)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                if (target != null) target.localScale = _baseBodyScale;
            })
            .OnKill(() =>
            {
                if (target != null) target.localScale = _baseBodyScale;
            });
    }

// Khi đủ khách → xoay xe, set sprite, nhả chỗ, chạy về Entrance
    private void BeginExit()
    {
        // visual quay đầu (giữ logic hiện tại của bạn)
        GameController.Instance.int_SortingNumberCarInParking--;
        isInSort = false;
        transform.rotation = Quaternion.Euler(0, -180f, 0);
        if (listSprite_CarDirections.Count > 0)
        {
            spriteRenderer_CarBody.sprite = listSprite_CarDirections[3]; // DownLeft sprite bạn đang dùng khi quay đầu
            spriteColliderSync.ApplyCurrentSpriteShape();
        }
        // mở cờ rời bãi (slot sẽ được nhả khi xe tới v2_EntrancePos trong Update)
        
        // ✅ bật cờ rời bãi để Update() MoveTowards(v2_EntrancePos)
        //isFulled = true;
    }
    
    private void CheckBlockByRaycast()
    {
        // 1) Lấy hướng
        Vector2 dir = GetDirectionVector(carInfo.directions_Car);
        if (dir == Vector2.zero) { isBlocked = false; return; }

        // 2) Bắn ray (lấy tất cả, sau đó lọc)
        Vector2 origin = _root != null ? (Vector2)_root.position : (Vector2)transform.position;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, float_GateRayCast, carLayer);

        // 3) Chọn va chạm hợp lệ gần nhất (khác “chiếc xe này”, tùy chọn bỏ qua trigger)
        RaycastHit2D bestHit = default;
        float bestDist = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            // ✅ Bỏ qua collider thuộc chiếc xe này
            if (_selfCols != null && _selfCols.Contains(hit.collider)) continue;
            // (hoặc) if (hit.collider.transform.IsChildOf(carRoot)) continue;

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                bestHit = hit;
            }
        }

        // 4) Kết luận & debug draw
        isBlocked = bestHit.collider != null;

        Debug.DrawRay(origin, dir * float_GateRayCast, Color.red);
        if (isBlocked)
        {
            Debug.DrawLine(origin, bestHit.point, Color.green);
            // Debug.Log($"Hit other: {bestHit.collider.name}");
        }
    }


    public Vector2 GetForwardDirection() => GetDirectionVector(carInfo != null ? carInfo.directions_Car : Directions.Up);

    public bool OwnsCollider(Collider2D col)
    {
        if (col == null) return false;
        if (_selfCols != null && _selfCols.Contains(col)) return true;
        return col.transform.IsChildOf(transform);
    }

    // Helper: đổi enum -> vector hướng (8 hướng)
    private Vector2 GetDirectionVector(Directions d)
    {
        switch (d)
        {
            case Directions.Up:        return Vector2.up;
            case Directions.UpLeft:    return new Vector2(-1, 1).normalized;
            case Directions.UpRight:   return new Vector2( 1, 1).normalized;
            case Directions.Right:     return Vector2.right;
            case Directions.Down:      return Vector2.down;
            case Directions.DownRight: return new Vector2( 1,-1).normalized;
            case Directions.DownLeft:  return new Vector2(-1,-1).normalized;
            case Directions.Left:      return Vector2.left;
            default:                   return Vector2.zero;
        }
    }
}
