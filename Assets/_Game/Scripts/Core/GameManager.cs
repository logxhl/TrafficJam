using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Header("-----------Data-----------")]
    public DataManager dataManager;
    [Header("-----------Refs-----------")]
    // public MaxAdsManager maxAdsManager;
    // public AdMobAppOpenManager adMobAppOpenManager;

    [Header("-----------Variables-----------")]
    public string sceneName = "SampleScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Gọi từ LoadingSceneController. Đảm bảo mọi data sẵn sàng trước khi vào gameplay.
    /// </summary>
    public IEnumerator InitializeAsync()
    {
        // Init các service/data 1 lần
        // adMobAppOpenManager.Init();
        // maxAdsManager.InitializeMaxSdk();
        dataManager.Init();
        dataManager.LoadData();

        yield return null;
        // Kết thúc init
    }

    // public void LoadScene(string sceneName)
    // {
    //     // Load the scene with the given name
    //     Time.timeScale = 1f;
    //     AudioManager.Ins.PlaySound_ButtonClick();
    //     SceneLoader.Load(sceneName);
    // }
    //
    // public IEnumerator LoadSceneWithTransition(string sceneName)
    // {
    //     animator_transition.gameObject.SetActive(true);
    //     animator_transition.SetTrigger("End");
    //     
    //     float start = Time.time;
    //     
    //     AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
    //     op.allowSceneActivation = false;
    //     while (op.progress < 0.9f)
    //         yield return null;
    //     float remain = 1f - (Time.time - start);
    //     if (remain > 0f) yield return new WaitForSeconds(remain);
    //     
    //     op.allowSceneActivation = true;
    //     animator_transition.SetTrigger("Start");
    // }
}