using UnityEngine;

public class CarGateClickable : MonoBehaviour
{
    private GarageController garageController;
    private CarControl carControl;

    public void Init(GarageController g)
    {
        garageController = g;
        carControl = GetComponent<CarControl>();
    }

    private void OnMouseUpAsButton()
    {
        if (garageController == null || carControl == null) return;

        // Cho xe rời vị trí chờ (tuỳ logic của bạn bên trong OnLeaveGate)
        // carControl.SetWaitingAtGate(false);
        // carControl.SetActiveControl(true);
        //carControl.OnLeaveGate();

        // Báo cho garage đẩy xe kế tiếp
        garageController.NotifyFrontCarLeft(carControl);

        // Xoá component để không click lặp
        Destroy(this);
    }
}