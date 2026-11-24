using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSave
{
    [Header("-----------Game Data-----------")]
    public bool isFirstLaunch; // Dùng để kiểm tra lần đầu mở game
    public string playerName;
    public bool isNew;
    public int int_Coins;
    public int level;
    public int best;
    public float soundVolume;
    public float musicVolume;
    public float vibrateAmount;

    [Header("-----------Quantity Abilities data-----------")] 
    public int int_ShuffleCount;
    public int int_SortCount;
    public int int_VipSlotCount;

    public GameSave() {
        isFirstLaunch = true;
        playerName = "You";
        isNew = true;
        int_Coins = 0;
        level = 0;
        best = 1;
        soundVolume = 1;
        musicVolume = 1;
        int_ShuffleCount = 2;
        int_SortCount = 2;
        int_VipSlotCount = 2;
    }
}