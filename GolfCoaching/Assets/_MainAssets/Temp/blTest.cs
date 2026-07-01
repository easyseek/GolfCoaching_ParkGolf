using UnityEngine;
using UnityEngine.UI;

public class blTest : MonoBehaviour
{
    [SerializeField] Button scanButton;
    [SerializeField] Button lockButton;

    [SerializeField] GameObject DeviceCardPrefab;
    [SerializeField] Transform DeviceCardRoot;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        blocked = BluetoothManager.Instance.IsSoftBlocked();
        Debug.Log(blocked ? "Bluetooth: Off" : "Bluetooth: On");

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

        BluetoothManager.OnDeviceFound += HandleDeviceFound;
        BluetoothManager.OnConfirmPasskey += HandleConfirmPasskey;

        ShowPairedDevices();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    bool blocked;


    private void HandleConfirmPasskey(string obj)
    {
        Debug.Log($"Call HandleConfirmPasskey({obj})");
        /*
        ModalWindow.Create().Init("Passkey", $"Confirm passkey: {obj}", ModalWindow.ModalType.YesNo, () =>
        {
            BluetoothManager.Instance.ConfirmPasskey();
        }, () => { });
        */
    }

    private void HandleDeviceFound((string macAddress, string name, bool paired) device)
    {
        
        int index;
        if (device.paired)
        {
            //index = transformPaired.GetSiblingIndex();
        }
        else
        {
            //index = transformFound.GetSiblingIndex();
        }

        GameObject deviceEntry = Instantiate(DeviceCardPrefab, DeviceCardRoot);
        deviceEntry.gameObject.SetActive(true);

        //deviceEntry.transform.SetSiblingIndex(index + 1);
        deviceEntry.GetComponent<BlDevice>().Init(new BluetoothDevice
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
