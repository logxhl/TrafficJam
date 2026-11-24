using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Create Cone Config", fileName = "ConeConfig")]
public class ConeConfig : ScriptableObject
{
    // ------------------- CÁC DANH SÁCH CẤU HÌNH -------------------

    [Header("Cone Info Configs")] public ObjectColor objectColor_cone;
    public Sprite sprite_Cone;
}