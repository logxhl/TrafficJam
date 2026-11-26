using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Serialization;

public class GarageController : MonoBehaviour
{
    [Header("----- Gate & UI -----")]
    [SerializeField] private Transform trans_GatePoint4Slot;      // vị trí đứng chờ trước cửa (4 slots)
    [SerializeField] private Transform trans_GatePoint10Slot;      // vị trí đứng chờ trước cửa (10 slots)
    [SerializeField] private TMP_Text text_CarCount;     // UI hiển thị số xe còn trong gara
    public SpriteRenderer SpriteRenderer_GarageSign;          // biểu tượng gara hiển thị trên mini map
    public GameObject obj_GarageDisplay;               // đối tượng hiển thị gara (có thể ẩn/hiện)
    public Transform trans_RayPoint;          // điểm bắn ray kiểm tra cửa (nếu có)

    [Header("----- Gate check (tuỳ chọn, để chắc chắn phía trước không kẹt) -----")]
    [SerializeField] private LayerMask carLayer;       // layer của xe
    [SerializeField] private float float_GateRayCast = 7f;
    
    [Header("----- Move settings -----")]
    [SerializeField] private float float_MoveDuration = 0.8f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    
    [Header("----- Preset cars in this garage (order = queue) -----")]
    [Tooltip("Kéo sẵn các CarControl đang nằm trong garage theo đúng thứ tự bạn muốn đẩy ra (trên xuống dưới/chứa nào trước thì đứng trước)")]
    public List<CarControl> list_PresetCars = new List<CarControl>();

    private readonly Queue<CarControl> queueInGarage = new Queue<CarControl>();
    private int int_LimitCarPush = 0;

    private static readonly Directions[] gateDirections =
    {
        Directions.Up, Directions.UpLeft, Directions.Left, Directions.DownLeft,
        Directions.Down, Directions.DownRight, Directions.Right, Directions.UpRight
    };

    public void ConfigureCars(IReadOnlyList<LevelData.GarageCar> cars, PrefabLibrary library)
    {
        // Each garage prefab has a SpawnPoint for the cars waiting inside it.
        var spawnPoint = transform.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            Debug.LogWarning("[GarageController] Missing SpawnPoint on " + name, this);
            return;
        }

        foreach (var preset in list_PresetCars)
        {
            if (preset == null) continue;
            preset.gameObject.SetActive(false);
            Destroy(preset.gameObject);
        }
        list_PresetCars.Clear();

        int sector = Mathf.RoundToInt(Vector2.SignedAngle(Vector2.up, transform.up) / 45f);
        var facing = gateDirections[(sector + gateDirections.Length) % gateDirections.Length];

        foreach (var placement in cars)
        {
            string key = placement.size switch
            {
                CarSize.Small4Slots => "carsmall",
                CarSize.Medium6Slots => "carmedium",
                CarSize.Big10Slots => "carbig",
                _ => null
            };
            var prefab = key != null ? library.Get(key) : null;
            var template = prefab != null ? prefab.GetComponent<CarControl>() : null;
            if (template == null || template.carInfo == null || template.carInfo.carSize != placement.size)
            {
                Debug.LogWarning("[GarageController] Missing car prefab for " + placement.size, this);
                continue;
            }

            var car = Instantiate(template, spawnPoint);
            car.gameObject.SetActive(false);
            car.transform.localPosition = Vector3.zero;
            car.transform.localScale = Vector3.one * 0.1f;
            car.carInfo.objectColor_car = placement.color;
            car.carInfo.directions_Car = facing;
            car.carInfo.carArea = CarArea.InGarage;
            list_PresetCars.Add(car);
        }
    }

    public IEnumerator HandleStart()
    {
        // Build queue theo thứ tự bạn đã kéo trong Inspector
        foreach (var c in list_PresetCars)
        {
            if (c == null) continue;
            c.SetActiveControl(false);
            c.SetWaitingAtGate(true);
            queueInGarage.Enqueue(c);
        }

        UpdateCountUI();
        TryPushNextToGate();
        yield return null;
    }

    private void Update()
    {
        if(int_LimitCarPush <= 0)
            TryPushNextToGate();
    }

    // ====== Core ======

    private bool IsGateFree()
    {
        if (trans_GatePoint4Slot == null || trans_GatePoint10Slot == null) return false;

        // Nếu bạn muốn bỏ hẳn physics check thì return true;
        RaycastHit2D hit = Physics2D.Raycast(trans_RayPoint.position, transform.up, float_GateRayCast, carLayer);

        if (hit.collider != null)
        {
            Debug.Log($"❌ Gate blocked by {hit.transform.name}");
            return false;
        }
        else
        {
            return true;
        }
    }

    private void TryPushNextToGate()
    {
        if (!IsGateFree()) return;
        if (queueInGarage.Count == 0) return;

        int_LimitCarPush++;
        var car = queueInGarage.Dequeue();
        UpdateCountUI();
        
        car.SetWaitingAtGate(true);
        car.SetActiveControl(true);       // khoá điều khiển khi đang ở cửa (chờ người chơi bấm)
        car.SetupVisuals();

        car.gameObject.SetActive(true);
        // car.transform.position = trans_GatePoint.position;
        // AttachGateClickable(car);
        
        DOTween.Kill(car.transform);

        Sequence seq = DOTween.Sequence();

        Vector2 desPos = new Vector2();
        if (car.carInfo.carSize == CarSize.Big10Slots)
        {
            desPos = trans_GatePoint10Slot.position; 
        } else
        {
            desPos = trans_GatePoint4Slot.position;
        }
        seq.Join(car.transform.DOMove(desPos, float_MoveDuration))
            .Join(car.transform.DOScale(Vector3.one, float_MoveDuration)) // tăng scale về (1,1,1)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                car.transform.position = desPos;
                AttachGateClickable(car);
            });
    }

    private void UpdateCountUI()
    {
        if (text_CarCount != null) text_CarCount.text = queueInGarage.Count.ToString();
        if (queueInGarage.Count > 0)
        {
            var carTop = queueInGarage.Peek();
            if (carTop != null && SpriteRenderer_GarageSign != null)
                SpriteRenderer_GarageSign.sprite = carTop.sprite_GarageSign;
        }
        else
        {
            obj_GarageDisplay.SetActive(false);
        }
    }

    private void AttachGateClickable(CarControl car)
    {
        var click = car.GetComponent<CarGateClickable>();
        if (click == null) click = car.gameObject.AddComponent<CarGateClickable>();
        click.Init(this);
    }

    // Được gọi bởi xe trước cửa khi người chơi bấm để xe rời đi
    public void NotifyFrontCarLeft(CarControl car)
    {
        int_LimitCarPush = 0;
        //TryPushNextToGate();
    }

    private void OnDrawGizmosSelected()
    {
        if (trans_GatePoint4Slot == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(trans_RayPoint.position, transform.up * float_GateRayCast);
    }
}
