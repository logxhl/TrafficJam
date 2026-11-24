using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Create List Cone Config", fileName = "ListConeConfigs")]
public class ConeConfigs : ScriptableObject
{
    public List<ConeConfig> listConeConfigs;
    
    public ConeConfig GetConeConfig(ObjectColor color)
    {
        foreach (var config in listConeConfigs)
        {
            if (config.objectColor_cone == color)
                return config;
        }
        Debug.LogWarning($"❌ Không tìm thấy ConeConfig cho màu {color}");
        return null;
    }
}