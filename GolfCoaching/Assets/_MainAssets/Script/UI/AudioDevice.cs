using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioDevice : MonoBehaviour
{
    [SerializeField] private BluetoothDevice device;

    [SerializeField] TextMeshProUGUI txtName;
    [SerializeField] Image imgIcon;
    [SerializeField] Button connectButton;
    [SerializeField] TextMeshProUGUI txtConnectStatus;

    [SerializeField] Color ColConnect;
    [SerializeField] Color ColDisconnect;

    bool connected;


    private void Awake()
    {
        connectButton.onClick.AddListener(() =>
        {
            if (connected)
            {
                BluetoothManager.Instance.DisconnectToDevice(device.macAddress);
            }
            else
             {
                BluetoothManager.Instance.ConnectToDevice(device.macAddress);
             }
        });

    }

    public void Init(BluetoothDevice newDevice)//, Action Connect, Action Disconnect)
    {
        device = newDevice;

        txtName.text = device.name;
        SetColor(false);

        UpdateStats();

        BluetoothManager.OnUpdateUI += UpdateStats;
        BluetoothManager.OnRemoveDevice += OnRemoveDevice;
    }

    void SetColor(bool isConnect)
    {
        txtName.color = isConnect ? ColConnect : Color.white;
        imgIcon.color = isConnect ? ColConnect : Color.white;
        txtConnectStatus.color = isConnect ? ColConnect : ColDisconnect;
        txtConnectStatus.text = connected ? "연결됨" : "연결 안 됨";
    }

    private void OnRemoveDevice(string obj)
    {
        if (obj == device.macAddress)
        {
            BluetoothManager.OnUpdateUI -= UpdateStats;
            BluetoothManager.OnRemoveDevice -= OnRemoveDevice;

            BluetoothManager.Instance.RemoveDeviceFromList(obj);
            Debug.Log("Removed: " + obj);
            Destroy(gameObject);
        }
    }


    public void UpdateStats()
    {
        if (this == null || gameObject == null)
            return;

        var manager = BluetoothManager.Instance;
        if (manager == null)
            return;
            
        BluetoothDeviceInfo info = BluetoothManager.Instance.GetDeviceInfo(device.macAddress);
        connected = info.connected;
        
        SetColor(connected);
        
    }
}
