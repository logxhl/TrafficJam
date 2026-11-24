using UnityEngine;
using UnityEngine.EventSystems;

public class TapToCloseUI : MonoBehaviour, IPointerDownHandler
{
    [Header("------ Panel cần ẩn ------")]
    public GameObject obj_PanelToHide; // Kéo panel popup vào đây

    public void OnPointerDown(PointerEventData eventData)
    {
        if (obj_PanelToHide != null)
            obj_PanelToHide.SetActive(false);
        else
            gameObject.SetActive(false);

        Time.timeScale = 1f;
        Debug.Log("Tap anywhere to close popup!");
    }
}