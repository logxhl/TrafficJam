using UnityEngine;

public class FPSManager : MonoBehaviour
{
    public int targetFPS = 60;

    private static FPSManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        //DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount = 0; // đảm bảo không bị vsync override
    }
}