// SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string STR_LoadingSceneName = "Loading";
    private static string str_NextSceneName;
    private static bool isHasPending;

    /// Gọi hàm này để load sang scene khác có qua màn loading.
    public static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName rỗng!");
            return;
        }
        if (sceneName == STR_LoadingSceneName)
        {
            Debug.LogError("[SceneLoader] Không thể đặt LoadingScene làm đích.");
            return;
        }

        str_NextSceneName = sceneName;
        GameManager.Instance.sceneName = sceneName;
        isHasPending = true;

        // Chuyển qua màn loading (single) để hiển thị UI và load async scene đích.
        SceneManager.LoadScene(STR_LoadingSceneName, LoadSceneMode.Single);
    }

    /// Được gọi từ Loading scene để lấy & xóa “tham số”.
    public static bool TryConsumeNext(out string sceneName)
    {
        if (isHasPending && !string.IsNullOrEmpty(str_NextSceneName))
        {
            sceneName = str_NextSceneName;
            isHasPending = false;
            str_NextSceneName = null;
            return true;
        }
        sceneName = null;
        return false;
    }
}