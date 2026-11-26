// Assets/_Game/Level/RuntimeLevelBuilderLite.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using DG.Tweening;
using UnityEditor;
#endif

public class RuntimeLevelBuilder : MonoBehaviour
{
    public List<LevelData> level;
    public LevelData levelData_Current;
    public PrefabLibrary library;
    public ConeConfigs coneConfigs;

    [Header("Parents in Scene")]
    public Transform parkingParent;
    public Transform conesParent;
    public Transform garagesParent;

    public bool autoBuildOnStart = true;

    private readonly List<GarageController> spawnedGarageControllers = new();
    public IReadOnlyList<GarageController> SpawnedGarages => spawnedGarageControllers;

#if UNITY_EDITOR
    private readonly Dictionary<LevelData.CarPlacement, CarControl> spawnedCars = new();
    private readonly Dictionary<LevelData.ConePlacement, ConeBlocker> spawnedCones = new();
    private readonly Dictionary<LevelData.GaragePlacement, Transform> spawnedGarages = new();
    private LevelData builtLevel;

    public void SaveCarPositionsToLevelData() => SaveLayoutToLevelData();

    [ContextMenu("Save Layout to LevelData")]
    public void SaveLayoutToLevelData()
    {
        if (!Application.isPlaying || builtLevel == null || builtLevel != levelData_Current ||
            !AssetDatabase.Contains(builtLevel))
        {
            Debug.LogWarning("[RuntimeLevelBuilder] Hãy chạy màn có LevelData trước khi lưu bố cục.", this);
            return;
        }

        bool hasChanges = false;
        void RegisterUndo()
        {
            if (hasChanges) return;
            Undo.RegisterCompleteObjectUndo(builtLevel, "Save Level Layout");
            hasChanges = true;
        }

        int carsChanged = 0;
        int carsSkipped = 0;
        foreach (var placement in builtLevel.cars)
        {
            if (parkingParent == null || !spawnedCars.TryGetValue(placement, out var car) ||
                car == null || car.carInfo == null ||
                !car.gameObject.activeInHierarchy || car.transform.parent != parkingParent ||
                car.HasLeftLot || car.isMoving || car.isParking || car.isParked ||
                car.parkingSpace_current != null || DOTween.IsTweening(car.transform))
            {
                carsSkipped++;
                continue;
            }

            var position = car.transform.position;
            var facing = car.carInfo.directions_Car;
            var color = car.carInfo.objectColor_car;
            if (placement.position.Equals(position) && placement.facing == facing &&
                placement.ObjectColor_car == color) continue;

            RegisterUndo();
            placement.position = position;
            placement.facing = facing;
            placement.ObjectColor_car = color;
            carsChanged++;
        }

        int conesChanged = 0;
        int conesSkipped = 0;
        foreach (var placement in builtLevel.cones)
        {
            if (conesParent == null || !spawnedCones.TryGetValue(placement, out var cone) ||
                cone == null || cone.transform.parent != conesParent)
            {
                conesSkipped++;
                continue;
            }

            var position = cone.transform.position;
            // initialCount is the starting quota, even after gameplay opens the cone.
            if (placement.position.Equals(position) && placement.count == cone.initialCount &&
                placement.coneColor == cone.coneColor) continue;

            RegisterUndo();
            placement.position = position;
            placement.count = cone.initialCount;
            placement.coneColor = cone.coneColor;
            conesChanged++;
        }

        int garagesChanged = 0;
        int garagesSkipped = 0;
        foreach (var placement in builtLevel.garages)
        {
            if (garagesParent == null || !spawnedGarages.TryGetValue(placement, out var garage) ||
                garage == null || garage.parent != garagesParent)
            {
                garagesSkipped++;
                continue;
            }

            var position = garage.position;
            if (placement.position.Equals(position)) continue;

            RegisterUndo();
            placement.position = position;
            garagesChanged++;
        }

        if (hasChanges)
        {
            EditorUtility.SetDirty(builtLevel);
            AssetDatabase.SaveAssetIfDirty(builtLevel);
        }

        Debug.Log($"[RuntimeLevelBuilder] {builtLevel.name}: đã lưu {carsChanged} xe, {conesChanged} cone, " +
                  $"{garagesChanged} garage. Bỏ qua {carsSkipped} xe, {conesSkipped} cone, {garagesSkipped} garage.",
                  builtLevel);
    }
#endif

    public IEnumerator HandleStart()
    {
        int levelIndex = DataManager.Ins?.Level ?? 0;
        levelData_Current = level[levelIndex];
        if (autoBuildOnStart) BuildNow();
        yield return null;
    }

    [ContextMenu("Build Now")]
    public void BuildNow()
    {
        if (level == null || library == null) { Debug.LogError("Missing LevelData/PrefabLibrary"); return; }
#if UNITY_EDITOR
        spawnedCars.Clear();
        spawnedCones.Clear();
        spawnedGarages.Clear();
        builtLevel = levelData_Current;
#endif
        Clear(parkingParent); Clear(conesParent); Clear(garagesParent);
        spawnedGarageControllers.Clear();
        GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot.Clear();

        foreach (var c in levelData_Current.cars)
        {
            var prefab = library.Get(c.key);
            if (!prefab) { Debug.LogWarning("Missing key: " + c.key); continue; }
            var go = Instantiate(prefab, parkingParent);
            go.transform.SetPositionAndRotation(c.position, Quaternion.Euler(0, 0, 0));

            var cc = go.GetComponent<CarControl>();
            if (cc != null)
            {
#if UNITY_EDITOR
                spawnedCars[c] = cc;
#endif
                if (cc.carInfo != null)
                {
                    cc.carInfo.directions_Car = c.facing;
                    cc.carInfo.objectColor_car = c.ObjectColor_car;
                }
                try { cc.SetupVisuals(); } catch { }
                //if (cc.spriteRenderer_CarBody != null) cc.spriteRenderer_CarBody.flipX = c.flipX;
            }
            GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot.Add(cc);
        }

        foreach (var co in levelData_Current.cones)
        {
            var pf = library.Get(co.key);
            if (!pf) { Debug.LogWarning("Missing key: " + co.key); continue; }
            var go = Instantiate(pf, conesParent);
            go.transform.position = co.position;
            var coneBlocker = go.GetComponent<ConeBlocker>();
            if (coneBlocker != null)
            {
                var config = coneConfigs.GetConeConfig(co.coneColor);
                if (config != null)
                {
                    coneBlocker.Setup(config, count: co.count);
#if UNITY_EDITOR
                    spawnedCones[co] = coneBlocker;
#endif
                }
            }
        }

        foreach (var g in levelData_Current.garages)
        {
            var pf = library.Get(g.key);
            if (!pf) { Debug.LogWarning("Missing key: " + g.key); continue; }
            var go = Instantiate(pf, garagesParent);
            go.transform.position = g.position;
#if UNITY_EDITOR
            spawnedGarages[g] = go.transform;
#endif

            var garageController = go.GetComponent<GarageController>();
            if (garageController != null)
            {
                if (g.overridePresetCars)
                    garageController.ConfigureCars(g.cars, library);

                spawnedGarageControllers.Add(garageController);
                GameController.Instance.garageController = garageController;
                foreach (var car in garageController.list_PresetCars)
                {
                    GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot.Add(car);
                }
            }
        }
    }

    void Clear(Transform t)
    {
        if (!t) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }
}
