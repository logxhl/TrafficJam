// Assets/_Game/Level/RuntimeLevelBuilderLite.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public IEnumerator HandleStart()
    {
        levelData_Current = level[DataManager.Ins.Level];
        if (autoBuildOnStart) BuildNow();
        yield return null;
    }

    [ContextMenu("Build Now")]
    public void BuildNow()
    {
        if (level == null || library == null) { Debug.LogError("Missing LevelData/PrefabLibrary"); return; }
        Clear(parkingParent); Clear(conesParent); Clear(garagesParent);
        GameController.Instance.parkingLotManager.listCarControl_CarInParkingLot.Clear();

        foreach (var c in levelData_Current.cars)
        {
            var prefab = library.Get(c.key);
            if (!prefab) { Debug.LogWarning("Missing key: " + c.key); continue; }
            var go = Instantiate(prefab, parkingParent);
            go.transform.SetPositionAndRotation(c.position, Quaternion.Euler(0,0,0));

            var cc = go.GetComponent<CarControl>();
            if (cc != null)
            {
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
