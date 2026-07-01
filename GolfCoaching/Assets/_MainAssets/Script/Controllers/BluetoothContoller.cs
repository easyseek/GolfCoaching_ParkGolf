using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BluetoothContoller : MonoBehaviour
{   //[SerializeField] Button scanButton;
    //[SerializeField] Button lockButton;

    [SerializeField] GameObject DeviceCardPrefab;
    [SerializeField] Transform DeviceCardRoot;
    [SerializeField] Toggle tglBluetoothSwitch;
    bool blocked;
    //bool isScan = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        blocked = BluetoothManager.Instance.IsSoftBlocked();
        Debug.Log(blocked ? "Bluetooth: Off" : "Bluetooth: On");
        tglBluetoothSwitch.SetIsOnWithoutNotify(!blocked);
        tglBluetoothSwitch.onValueChanged.AddListener(OnValueChanged_BluetoothSwitch);
/*
        scanButton.onClick.AddListener(() => 
        {
            BluetoothManager.Instance.SetScan(true);
            Debug.Log("Scanning...");
        });
        lockButton.onClick.AddListener(() =>
        {
            blocked = !blocked;
            BluetoothManager.Instance.SetBluetoothBlock(blocked);
            Debug.Log(blocked ? "Bluetooth: Off" : "Bluetooth: On");
        });
*/
        BluetoothManager.OnDeviceFound += HandleDeviceFound;
        BluetoothManager.OnConfirmPasskey += HandleConfirmPasskey;

        //ShowPairedDevices();

        
    }

    public void StartScan()
    {
        BluetoothManager.Instance.SetScan(true);
            Debug.Log("Scanning...");
    }

    public void ReStartScan()
    {
        StartCoroutine(CoReStartScan());
    }

    IEnumerator CoReStartScan()
    {
        BluetoothManager.Instance.SetScan(false);
        BluetoothManager.Instance.RemoveDeviceAll();
        Debug.Log("Scanning Stop");

        yield return null;

        if(DeviceCardRoot.childCount > 0)
        {
            for(int i = 0; i < DeviceCardRoot.childCount; i++)
                Destroy(DeviceCardRoot.GetChild(i).gameObject);
        }

        yield return null;

        BluetoothManager.Instance.SetScan(true);
            Debug.Log("Scanning...");
    }

    public void OnValueChanged_BluetoothSwitch(bool isOn)
    {
        BluetoothManager.Instance.SetBluetoothBlock(!isOn);
    }


    private void HandleConfirmPasskey(string obj)
    {
        Debug.Log($"Call HandleConfirmPasskey({obj})");
    }

    private void HandleDeviceFound((string macAddress, string name, bool paired) device)
    {
        
        int index;

        GameObject deviceEntry = Instantiate(DeviceCardPrefab, DeviceCardRoot);
        deviceEntry.gameObject.SetActive(true);

        //deviceEntry.transform.SetSiblingIndex(index + 1);
        deviceEntry.GetComponent<AudioDevice>().Init(new BluetoothDevice
        {
            name = device.name,
            macAddress = device.macAddress
        });
        
        Debug.Log($"device name:{device.name} / {device.macAddress}");
    }

    private void ShowPairedDevices()
    {
        var devices = BluetoothManager.Instance.ListPairedDevices();
        foreach (var device in devices)
        {
            BluetoothManager.Instance.HandleNewDevice(device.name, device.macAddress, true);
        }
    }
}
