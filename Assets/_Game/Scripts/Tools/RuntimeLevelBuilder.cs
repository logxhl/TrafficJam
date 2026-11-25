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

#if UNITY_EDITOR
    private readonly Dictionary<LevelData.CarPlacement, CarControl> spawnedCars = new();
    private LevelData builtLevel;

    [ContextMenu("Save Car Positions to LevelData")]
    public void SaveCarPositionsToLevelData()
    {
        if (!Application.isPlaying || builtLevel == null || builtLevel != levelData_Current ||
            parkingParent == null || !AssetDatabase.Contains(builtLevel))
        {
            Debug.LogWarning("[RuntimeLevelBuilder] Hãy chạy màn có LevelData trước khi lưu vị trí xe.", this);
            return;
        }

        int changed = 0;
        int skipped = 0;
        foreach (var placement in builtLevel.cars)
        {
            if (!spawnedCars.TryGetValue(placement, out var car) || car == null ||
                !car.gameObject.activeInHierarchy || car.transform.parent != parkingParent ||
                car.HasLeftLot || car.isMoving || car.isParking || car.isParked ||
                car.parkingSpace_current != null || DOTween.IsTweening(car.transform))
            {
                skipped++;
                continue;
            }

            var position = car.transform.position;
            if (placement.position.Equals(position)) continue;

            if (changed == 0) Undo.RegisterCompleteObjectUndo(builtLevel, "Save Car Positions");
            placement.position = position;
            changed++;
        }

        if (changed > 0)
        {
            EditorUtility.SetDirty(builtLevel);
            AssetDatabase.SaveAssetIfDirty(builtLevel);
        }

        Debug.Log($"[RuntimeLevelBuilder] {builtLevel.name}: đã lưu vị trí {changed} xe, bỏ qua {skipped} xe.", builtLevel);
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
        builtLevel = levelData_Current;
#endif
        Clear(parkingParent); Clear(conesParent); Clear(garagesParent);
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
            // TODO: nếu có ConeController → gán màu/count tại đây
            var coneBlocker = go.GetComponent<ConeBlocker>();
            if (coneBlocker != null)
            {
                var config = coneConfigs.GetConeConfig(co.coneColor);
                if (config != null)
                {
                    coneBlocker.Setup(config, count: co.count);
                }
            }
        }

        foreach (var g in levelData_Current.garages)
        {
            var pf = library.Get(g.key);
            if (!pf) { Debug.LogWarning("Missing key: " + g.key); continue; }
            var go = Instantiate(pf, garagesParent);
            go.transform.position = g.position;

            var garageController = go.GetComponent<GarageController>();
            if (garageController != null)
            {
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
