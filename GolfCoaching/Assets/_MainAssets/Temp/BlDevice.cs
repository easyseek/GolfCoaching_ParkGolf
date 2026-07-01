using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlDevice : MonoBehaviour
{
    [SerializeField] private BluetoothDevice device;

    [SerializeField] TextMeshProUGUI txtName;
    [SerializeField] TextMeshProUGUI txtMacAddr;
    [SerializeField] Button connectButton;
    [SerializeField] TextMeshProUGUI txtConnectButton;

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
        txtMacAddr.text = device.macAddress;
        UpdateStats();

        BluetoothManager.OnUpdateUI += UpdateStats;
        BluetoothManager.OnRemoveDevice += OnRemoveDevice;
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
        //DebugText.text = $"Paired: {info.paired}, Trusted: {info.trusted}, Bonded: {info.bonded}, Connected: {info.connected}, Battery: {info.batteryPercentage}";
        //paired = info.paired;
        connected = info.connected;
        //trusted = info.trusted;

        //connectButton.interactable = paired;

        txtConnectButton.text = connected ? "Disconnect" : "Connect";
        //pairButtonText.text = paired ? "Remove" : "Pair";
        //trustedButtonText.text = trusted ? "Untrust" : "Trust";
    }
}
