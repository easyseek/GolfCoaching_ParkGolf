using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    [Header("* Base Setting")]
    [SerializeField] Button btnBaseSetting;

    [SerializeField] TextMeshProUGUI txtFrontCamID;
    [SerializeField] TextMeshProUGUI txtSideCamID;

    [Header("* Bluetooth")]
    [SerializeField] GameObject Panel_SearchDevice;
    [SerializeField] BluetoothContoller bluetoothContoller;
    [SerializeField] Toggle tglBluetoothSwitch;
    bool blocked;


    void Start()
    {
        //CAMERA
        txtFrontCamID.text = Utillity.Instance.frontCameraID.ToString();
        txtSideCamID.text = Utillity.Instance.sideCameraID.ToString();

        //블루투스
        blocked = BluetoothManager.Instance.IsSoftBlocked();
        Debug.Log(blocked ? "Bluetooth: Off" : "Bluetooth: On");
        tglBluetoothSwitch.SetIsOnWithoutNotify(!blocked);
        tglBluetoothSwitch.onValueChanged.AddListener(OnValueChanged_BluetoothSwitch);
    }

    public void OnClick_ShowSearchPanel(bool isShow)
    {
        Panel_SearchDevice.SetActive(isShow);
        bluetoothContoller.StartScan();
    }

    public void OnValueChanged_BluetoothSwitch(bool isOn)
    {
        BluetoothManager.Instance.SetBluetoothBlock(isOn);
    }

    public void OnClick_CameraSwap()
    {
        int temp = Utillity.Instance.frontCameraID;
        Utillity.Instance.frontCameraID = Utillity.Instance.sideCameraID;
        Utillity.Instance.sideCameraID = temp;

        txtFrontCamID.text = Utillity.Instance.frontCameraID.ToString();
        txtSideCamID.text = Utillity.Instance.sideCameraID.ToString();
        Utillity.Instance.SetConfig();
    }


}
