using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour {
    public static DataManager Ins;
    public bool isLoaded = false;
    public GameSave gameSave;
    public GameSave gameSave_BackUp;

    void OnApplicationPause(bool pause) {
        if (pause == true)
            SaveGame();
    }
    void OnApplicationQuit() { SaveGame(); }

    void Reset() {
        gameSave.isNew = true;
    }

    public void Init() {
        if (Ins == null) {
            Ins = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public void LoadData() {
        if (isLoaded == false) {
            if (PlayerPrefs.HasKey("GameSave"))
                gameSave = JsonUtility.FromJson<GameSave>(PlayerPrefs.GetString("GameSave"));
            if (gameSave.isNew) {
                InitData();
            }
            isLoaded = true;
        }
    }

    public void SaveGame() {
        try {
            if (!isLoaded)
                return;
            if (gameSave == null) {
                if (gameSave_BackUp != null) {
                    gameSave = gameSave_BackUp;
                    Debug.LogError("gameSave bị null , backup thành công ");
                } else {
                    InitData();
                    Debug.LogError("gameSave bị null , backup không thành công . Reset data");
                }
            }
            gameSave_BackUp = gameSave;
            PlayerPrefs.SetString("GameSave", JsonUtility.ToJson(gameSave));
            PlayerPrefs.Save();
        } catch (Exception ex) {
            Debug.LogError("Lỗi LoadData:" + ex);
        }
    }

    void InitData() {
        gameSave = new GameSave();
        gameSave.isNew = false;
    }

    // ============================================================
    // ================    API ĐỌC / GHI DỮ LIỆU    ===============
    // ============================================================

    // --------- Coin ---------
    public int Coin => gameSave?.int_Coins ?? 0;
    public void SetCoin(int value)
    {
        gameSave.int_Coins = Mathf.Max(0, value);
        SaveGame();
    }
    public void AddCoin(int delta)
    {
        long sum = (long)gameSave.int_Coins + delta;
        gameSave.int_Coins = (int)Mathf.Max(0, sum);
        SaveGame();
    }
    public bool TryMinusCoin(int delta)
    {
        if (gameSave.int_Coins >= delta)
        {
            gameSave.int_Coins -= delta;
            SaveGame();
            return true;
        }
        return false;
    }

    // --------- Level / Best ---------
    public bool IsFirstLaunch => gameSave?.isFirstLaunch ?? true;
    public void SetFirstLaunch(bool isFirst)
    {
        gameSave.isFirstLaunch = isFirst;
        SaveGame();
    }
    public int Level => gameSave?.level ?? 0;
    public void SetLevel(int level)
    {
        gameSave.level = Mathf.Max(0, level);
        SaveGame();
    }
    public void NextLevel(int levelCount)
    {
        if (levelCount <= 0) return;

        gameSave.level = Level < levelCount - 1 ? Level + 1 : 0;
        SaveGame();
    }

    public int Best => gameSave?.best ?? 0;

    public void SetBestRank(int score)
    {
        gameSave.best = score;
        SaveGame();
    }
    
    // --------- Player Name ---------
    public string PlayerName => gameSave?.playerName ?? "You";
    public void SetPlayerName(string name)
    {
        gameSave.playerName = name;
        SaveGame();
    }

    // --------- Volume (Âm thanh) ---------
    public float SoundVolume => gameSave?.soundVolume ?? 1f;
    public float MusicVolume => gameSave?.musicVolume ?? 0f;
    public float VibrateAmount => gameSave?.vibrateAmount ?? 0f;

    public void SetSoundVolume(float v)
    {
        gameSave.soundVolume = Mathf.Clamp01(v);
        SaveGame();
    }
    public void SetMusicVolume(float v)
    {
        gameSave.musicVolume = Mathf.Clamp01(v);
        SaveGame();
    }
    
    public void UpdateVibrateAmount(float amount) {
        gameSave.vibrateAmount = amount;
        SaveGame();
    }

    // --------- Abilities ---------
    public int ShuffleCount   => gameSave?.int_ShuffleCount   ?? 0;
    public int SortCount      => gameSave?.int_SortCount      ?? 0;
    public int VipSlotCount   => gameSave?.int_VipSlotCount   ?? 0;
    public void AddShuffle(int delta)
    {
        long sum = (long)gameSave.int_ShuffleCount + delta;
        gameSave.int_ShuffleCount = (int)Mathf.Max(0, sum);
        SaveGame();
    }
    public void MinusShuffle()
    {
        long sum = (long)gameSave.int_ShuffleCount - 1;
        gameSave.int_ShuffleCount = (int)Mathf.Max(0, sum);
        SaveGame();
    }

    public void AddSort(int delta)
    {
        long sum = (long)gameSave.int_SortCount + delta;
        gameSave.int_SortCount = (int)Mathf.Max(0, sum);
        SaveGame();
    }
    public void MinusSort()
    {
        long sum = (long)gameSave.int_SortCount - 1;
        gameSave.int_SortCount = (int)Mathf.Max(0, sum);
        SaveGame();
    }
    public void AddVipSlot(int delta)
    {
        long sum = (long)gameSave.int_VipSlotCount + delta;
        gameSave.int_VipSlotCount = (int)Mathf.Max(0, sum);
        SaveGame();
    }
    public void MinusVipSlot()
    {
        long sum = (long)gameSave.int_VipSlotCount - 1;
        gameSave.int_VipSlotCount = (int)Mathf.Max(0, sum);
        SaveGame();
    }
}
