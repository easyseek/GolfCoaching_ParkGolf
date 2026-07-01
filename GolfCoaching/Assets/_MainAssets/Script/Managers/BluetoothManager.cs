using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

public class BluetoothManager : MonoBehaviourSingleton<BluetoothManager>
{

    //Player
    public static event Action OnPlayerPlaying;
    public static event Action OnPlayerPaused;
    public static event Action<string> OnPlayerTitleChanged;
    public static event Action<string> OnPlayerArtistChanged;
    public static event Action OnPlayerStopped;

    //UI
    public static event Action OnUpdateUI;
    public static event Action<string> OnRemoveDevice;
    //public static event Action<string, bool> OnDeviceFound;
    public static event Action<(string macAddress, string name, bool paired)> OnDeviceFound;

    //Connection
    public static event Action<string> OnDeviceConnected;
    public static event Action OnFailedToConnected;
    public static event Action<string> OnDeviceDisconnected;
    public static event Action<string> OnDeviceNotAvailable;
    public static event Action<string> OnConfirmPasskey;

    private Process process;
    private StreamWriter processInputWriter;

    [SerializeField] private List<BluetoothDevice> bluetoothDevices = new List<BluetoothDevice>();

    private string _connectedAudioMacAddress = string.Empty;
    private string _connectedAudioSourceName = string.Empty;

    IEnumerator Start()
    {
        bool ret = StartBluetoothCtl();

        if (ret)
        {
            var devices = ListConnectedDevices();

            if (devices.Count > 0)
            {
                OnDeviceConnected?.Invoke(devices[0].macAddress);
                UnityEngine.Debug.Log($"<b><color=green>Connected to: {devices[0].macAddress}</color></b>");
            }

            yield return null;

            // 시작할 때 무조건 기존 기록 제거
            ClearAllPairedDevices();
        }
    }

    private void ClearAllPairedDevices()
    {
        var paired = ListAllDevices();//ListPairedDevices();

        foreach (var device in paired)
        {
            SendCommand($"remove {device.macAddress}");
        }
    }


    public bool IsSoftBlocked()
    {
        //string input = LinuxCommand.Run("rfkill list");
        string input = LinuxCommand.RunDirect("/usr/sbin/rfkill", "list");
        bool softLock = BluetoothParser.IsSoftBlocked(input);
        return softLock;
    }

    public void SetBluetoothBlock(bool blocked)
    {
        //string command = blocked ? "rfkill block bluetooth" : "rfkill unblock bluetooth";
        //LinuxCommand.Run(command);
        string command = blocked ? "block bluetooth" : "unblock bluetooth";
        LinuxCommand.RunDirect("/usr/sbin/rfkill", command);

        UnityEngine.Debug.Log(blocked ? "Bluetooth: Off" : "Bluetooth: On");
    }

    public void SetScan(bool on)
    {
        string command = on ? "scan on" : "scan off";

        if (on)
            ClearAllPairedDevices();

        SendCommand(command);
    }

    public void SetPower(bool on)
    {
        string command = on ? "power on" : "power off";
        SendCommand(command);
    }

    public void SetDiscoverable(bool on)
    {
        string command = on ? "discoverable on" : "discoverable off";
        SendCommand(command);
    }

    public void SetDiscoverableTimeout(int seconds)
    {
        string command = $"discoverable-timeout {seconds}";
        SendCommand(command);
    }

    public void SetPairable(bool on)
    {
        string command = on ? "pairable on" : "pairable off";
        SendCommand(command);
    }

    public void SetAlias(string alias)
    {
        SendCommand($"set-alias {alias}");
    }

    public void ConnectToDevice(string deviceAddress)
    {
        SendCommand($"connect {deviceAddress}");
    }

    public void DisconnectToDevice(string deviceAddress)
    {
        SendCommand($"disconnect {deviceAddress}");
    }

    public void PairDevice(string deviceAddress)
    {
        SendCommand($"pair {deviceAddress}");
    }

    public void RemoveDevice(string deviceAddress)
    {
        SendCommand($"remove {deviceAddress}");
    }

    public void TrustDevice(string deviceAddress)
    {
        SendCommand($"trust {deviceAddress}");
    }

    public void UntrustDevice(string deviceAddress)
    {
        SendCommand($"untrust {deviceAddress}");
    }

    public void ConfirmPasskey()
    {
        UnityEngine.Debug.Log("Confirmed Passkey");
        SendCommand("yes");
    }

    public void DenyPasskey()
    {
        UnityEngine.Debug.Log("Denied Passkey");
        SendCommand("no");
    }

    public List<BluetoothDevice> ListPairedDevices()
    {
        //string devices = LinuxCommand.Run("bluetoothctl devices Paired");
        string devices = LinuxCommand.RunDirect("/usr/bin/bluetoothctl", "devices Paired");
        return BluetoothParser.ParseDevices(devices);
    }

    public List<BluetoothDevice> ListBondedDevices()
    {
        //string devices = LinuxCommand.Run("bluetoothctl devices Bonded");
        string devices = LinuxCommand.RunDirect("/usr/bin/bluetoothctl", "devices Bonded");
        return BluetoothParser.ParseDevices(devices);
    }

    public List<BluetoothDevice> ListTrustedDevices()
    {
        //string devices = LinuxCommand.Run("bluetoothctl devices Trusted");
        string devices = LinuxCommand.RunDirect("/usr/bin/bluetoothctl", "devices Trusted");
        return BluetoothParser.ParseDevices(devices);
    }

    public List<BluetoothDevice> ListConnectedDevices()
    {
        //string devices = LinuxCommand.Run("bluetoothctl devices Connected");
        string devices = LinuxCommand.RunDirect("/usr/bin/bluetoothctl", "devices Connected");
        return BluetoothParser.ParseDevices(devices);
    }

    public List<BluetoothDevice> ListAllDevices()
    {
        string devices = LinuxCommand.RunDirect("/usr/bin/bluetoothctl", "devices");
        return BluetoothParser.ParseDevices(devices);
    }    

    public BluetoothDeviceInfo GetDeviceInfo(string deviceAddress)
    {
        //string info = LinuxCommand.Run($"bluetoothctl info {deviceAddress}");
        string info = LinuxCommand.RunDirect("/usr/bin/bluetoothctl", $"info {deviceAddress}");
        return BluetoothParser.ParseDeviceInfo(info);
    }

    public void RemoveDeviceFromList(string deviceAddress)
    {
        bluetoothDevices.RemoveAll(device => device.macAddress == deviceAddress);
    }

    public void RemoveDeviceAll()
    {
        bluetoothDevices.Clear();
    }

    public void PlayerPlay()
    {
        SendPlayerCommand("play");
    }

    public void PlayerPause()
    {
        SendPlayerCommand("pause");
    }

    public void PlayerNext()
    {
        SendPlayerCommand("next");
    }

    public void PlayerPrevious()
    {
        SendPlayerCommand("previous");
    }

    public bool StartBluetoothCtl()
    {
        try
        {
            // Check if the system is running on Linux
            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
            {
                UnityEngine.Debug.Log("Unsupported platform: This function is intended for Linux systems only.");
                return false;
            }

            // Create process start info
            ProcessStartInfo psi = new ProcessStartInfo("bluetoothctl");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            // Start the process
            process = new Process();
            process.StartInfo = psi;
            process.Start();

            // Read the output
            process.OutputDataReceived += OutputDataReceived;
            process.ErrorDataReceived += ErrorDataReceived;

            processInputWriter = process.StandardInput;

            process.BeginOutputReadLine();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() => OutputDataReceivedMainThread(e.Data));
    }

    private void OutputDataReceivedMainThread(string input)
    {
        EventType eventType = BluetoothParser.ParseEventType(input);
        switch (eventType)
        {
            case EventType.NEW_Device:
                var (macAddress, deviceName) = BluetoothParser.ExtractDeviceInfo(input);

                //UnityEngine.Debug.Log($"New device: {deviceName}");
                if (macAddress != null)
                {
                    //HandleNewDevice(deviceName, macAddress, false);
                    HandleNewDeviceFiltered(deviceName, macAddress);
                }
                break;
            case EventType.NEW_Transport:
                //UnityEngine.Debug.Log("New Transport");
                break;
            case EventType.DEL_Device:
                //UnityEngine.Debug.Log("Device deleted");
                OnRemoveDevice?.Invoke(BluetoothParser.ExtractDeviceInfo(input).macAddress);
                break;
            case EventType.DEL_Transport:
                //UnityEngine.Debug.Log("Transport deleted");
                break;
            case EventType.CHG_Device:
                //UnityEngine.Debug.Log("Device changed");
                HandleDeviceChange(input);
                break;
            case EventType.CHG_Transport:
                //UnityEngine.Debug.Log("Transport changed");
                break;
            case EventType.CHG_Player:
                HandlePlayerChange(input);
                break;
            default:
                break;
        }


        if (input.Contains("Confirm passkey"))
        {
            //SendCommand("yes");
            OnConfirmPasskey?.Invoke(BluetoothParser.ExtractPasskey(input));
            return;
        }
        else if (input.Contains("Failed to connect"))
        {
            OnFailedToConnected?.Invoke();
            return;
        }
        else if (input.Contains("not available"))
        {
            OnDeviceNotAvailable?.Invoke(BluetoothParser.ExtractDeviceMac(input));
            return;
        }
    }

    private void HandleDeviceChange(string input)
    {
        OnUpdateUI?.Invoke();

        var (macAddress, connected, success) = BluetoothParser.ParseDeviceConnection(input);

        if (!success)
            return;

        if (connected)
        {
            _connectedAudioMacAddress = macAddress;

            OnDeviceConnected?.Invoke(macAddress);
            UnityEngine.Debug.Log($"<b><color=green>Connected to: {macAddress}</color></b>");

            SetScan(false);

            StartCoroutine(CoSetupBluetoothAudioInput(macAddress));
        }
        else
        {
            if (_connectedAudioMacAddress == macAddress)
            {
                _connectedAudioMacAddress = string.Empty;
                _connectedAudioSourceName = string.Empty;
            }

            OnDeviceDisconnected?.Invoke(macAddress);
            UnityEngine.Debug.Log($"<b><color=red>Disconnected from: {macAddress}</color></b>");

            StartCoroutine(CoSetScan(true));
            //SetScan(true);
            //ClearAllPairedDevices();
        }
    }

    private string RunPactl(string args)
    {
        string result = LinuxCommand.RunDirect("pactl", args);

        if (!string.IsNullOrEmpty(result) && result.Contains("Process error"))
        {
            UnityEngine.Debug.LogWarning("[BT-AUDIO] pactl failed by PATH. Retry /usr/bin/pactl. args=" + args);
            result = LinuxCommand.RunDirect("/usr/bin/pactl", args);
        }

        if (!string.IsNullOrEmpty(result) && result.Contains("Process error"))
        {
            UnityEngine.Debug.LogWarning("[BT-AUDIO] /usr/bin/pactl failed. Retry /bin/pactl. args=" + args);
            result = LinuxCommand.RunDirect("/bin/pactl", args);
        }

        if (!string.IsNullOrEmpty(result) && result.Contains("Process error"))
        {
            UnityEngine.Debug.LogWarning("[BT-AUDIO] pactl all paths failed. args=" + args + "\n" + result);
        }

        return result;
    }

    private IEnumerator CoSetupBluetoothAudioInput(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress))
            yield break;

        yield return new WaitForSeconds(1.0f);

        string sourceName = string.Empty;

        for (int i = 0; i < 8; i++)
        {
            sourceName = FindBluetoothInputSource(macAddress);

            if (!string.IsNullOrEmpty(sourceName))
                break;

            TrySwitchBluetoothProfileToHeadset(macAddress);

            yield return new WaitForSeconds(0.5f);
        }

        if (string.IsNullOrEmpty(sourceName))
        {
            UnityEngine.Debug.LogWarning("[BT-AUDIO] bluez input source not found. mac=" + macAddress);
            LogAudioSources();
            yield break;
        }

        _connectedAudioSourceName = sourceName;

        string setDefaultResult = RunPactl($"set-default-source {sourceName}");
        UnityEngine.Debug.Log("[BT-AUDIO] set-default-source: " + sourceName + " / " + setDefaultResult);

        string moveResult = MoveCaptureInputsToSource(sourceName);
        UnityEngine.Debug.Log("[BT-AUDIO] move capture inputs result: " + moveResult);

        string currentDefault = RunPactl("get-default-source");
        UnityEngine.Debug.Log("[BT-AUDIO] current default source: " + currentDefault);
    }

    private string FindBluetoothInputSource(string macAddress)
    {
        string sourceList = RunPactl("list sources short");
        UnityEngine.Debug.Log("[BT-AUDIO] sources:\n" + sourceList);

        if (string.IsNullOrEmpty(sourceList))
            return string.Empty;

        string macUnderscore = macAddress.Replace(":", "_").ToUpperInvariant();
        string macLower = macAddress.Replace(":", "_").ToLowerInvariant();

        string[] lines = sourceList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("bluez_input") &&
                (line.Contains(macUnderscore) || line.Contains(macLower)))
            {
                string[] cols = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (cols.Length >= 2)
                {
                    UnityEngine.Debug.Log("[BT-AUDIO] found input source: " + cols[1]);
                    return cols[1];
                }
            }
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("bluez_input"))
            {
                string[] cols = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (cols.Length >= 2)
                {
                    UnityEngine.Debug.Log("[BT-AUDIO] found fallback bluez input source: " + cols[1]);
                    return cols[1];
                }
            }
        }

        return string.Empty;
    }

    private void TrySwitchBluetoothProfileToHeadset(string macAddress)
    {
        string cardName = FindBluetoothCardName(macAddress);

        if (string.IsNullOrEmpty(cardName))
        {
            UnityEngine.Debug.LogWarning("[BT-AUDIO] bluetooth card not found. mac=" + macAddress);
            return;
        }

        string[] profiles =
        {
        "headset-head-unit-msbc",
        "handsfree_head_unit",
        "headset_head_unit",
        "headset-head-unit-cvsd"
        };

        for (int i = 0; i < profiles.Length; i++)
        {
            string result = RunPactl($"set-card-profile {cardName} {profiles[i]}");
            UnityEngine.Debug.Log($"[BT-AUDIO] set-card-profile {cardName} {profiles[i]} / {result}");

            string sourceName = FindBluetoothInputSource(macAddress);

            if (!string.IsNullOrEmpty(sourceName))
                return;
        }
    }

    private string FindBluetoothCardName(string macAddress)
    {
        string cardList = RunPactl("list cards short");
        UnityEngine.Debug.Log("[BT-AUDIO] cards:\n" + cardList);

        if (string.IsNullOrEmpty(cardList))
            return string.Empty;

        string macUnderscore = macAddress.Replace(":", "_").ToUpperInvariant();
        string macLower = macAddress.Replace(":", "_").ToLowerInvariant();

        string[] lines = cardList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("bluez_card") &&
                (line.Contains(macUnderscore) || line.Contains(macLower)))
            {
                string[] cols = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (cols.Length >= 2)
                {
                    UnityEngine.Debug.Log("[BT-AUDIO] found card: " + cols[1]);
                    return cols[1];
                }
            }
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("bluez_card"))
            {
                string[] cols = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (cols.Length >= 2)
                {
                    UnityEngine.Debug.Log("[BT-AUDIO] fallback card: " + cols[1]);
                    return cols[1];
                }
            }
        }

        return string.Empty;
    }

    private string MoveCaptureInputsToSource(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
            return string.Empty;

        string inputs = RunPactl("list source-outputs short");

        if (string.IsNullOrEmpty(inputs))
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        string[] lines = inputs.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (cols.Length <= 0)
                continue;

            string id = cols[0];
            string result = RunPactl($"move-source-output {id} {sourceName}");
            sb.AppendLine($"{id}: {result}");
        }

        return sb.ToString();
    }

    private void LogAudioSources()
    {
        string cards = RunPactl("list cards short");
        string sources = RunPactl("list sources short");
        string defaultSource = RunPactl("get-default-source");

        UnityEngine.Debug.Log("[BT-AUDIO] cards:\n" + cards);
        UnityEngine.Debug.Log("[BT-AUDIO] sources:\n" + sources);
        UnityEngine.Debug.Log("[BT-AUDIO] default source:\n" + defaultSource);
    }

    IEnumerator CoSetScan(bool isOn)
    {
        yield return new WaitForSeconds(1f);
        SetScan(isOn);
    }

    private void HandlePlayerChange(string input)
    {
        if (input.Contains("paused"))
        {
            OnPlayerPaused?.Invoke();

            UnityEngine.Debug.Log("Player paused");
            return;
        }
        else if (input.Contains("playing"))
        {
            OnPlayerPlaying?.Invoke();
            UnityEngine.Debug.Log("Player playing");
            return;
        }
        else if (input.Contains("stopped"))
        {
            OnPlayerStopped?.Invoke();
            UnityEngine.Debug.Log("Player Stopped (HIDE)");
            return;
        }

        var (title, artist) = BluetoothParser.ParseArtistAndSongTitle(input);

        if (!string.IsNullOrEmpty(title))
        {
            UnityEngine.Debug.Log($"Player title changed to: {title}");
            OnPlayerTitleChanged?.Invoke(title);
            return;
        }
        else if (!string.IsNullOrEmpty(artist))
        {
            UnityEngine.Debug.Log($"Player artist changed to: {artist}");
            OnPlayerArtistChanged?.Invoke(artist);
            return;
        }
    }

    public void HandleNewDevice(string name, string macAddress, bool paired)
    {
        if (string.IsNullOrEmpty(macAddress)) { return; }

        if (!bluetoothDevices.Exists(device => device.macAddress == macAddress))
        {
            bluetoothDevices.Add(new BluetoothDevice
            {
                name = name,
                macAddress = macAddress
            });

            OnDeviceFound?.Invoke((macAddress, name, paired));
            UnityEngine.Debug.Log($"<b>Found: {name}</b>");
        }
        else
        {
            OnUpdateUI?.Invoke();
        }
    }

    private async void HandleNewDeviceFiltered(string name, string macAddress)
    {
        // 1. 이름이 MAC 주소면 제외
        if (IsMacAddress(name))
            return;

        // 2. 장치 정보 조회
        //string info = LinuxCommand.Run($"bluetoothctl info {macAddress}");
        string info = LinuxCommand.RunDirect("/usr/bin/bluetoothctl", $"info {macAddress}");


        // 3. 오디오 장치가 아니면 제외
        if (!BluetoothParser.IsAudioDevice(info))
            return;

        // 4. 조건 통과 → 등록
        HandleNewDevice(name, macAddress, false);
    }

    public void SendPlayerCommand(string command)
    {
        SendCommand("menu player");
        SendCommand(command);
        SendCommand("back");
    }

    public void SendCommand(string command)
    {
        if (process != null && !process.HasExited && processInputWriter != null)
        {
            processInputWriter.WriteLine(command);
            processInputWriter.Flush();  // Ensure the command is sent immediately
        }
    }

    void OnDestroy()
    {
        if (process != null && !process.HasExited)
        {
            process.Kill();
        }

        if (processInputWriter != null)
        {
            processInputWriter.Close();
            processInputWriter = null;
        }
    }
    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        UnityEngine.Debug.LogError(e.Data);
    }

    private bool IsMacAddress(string name)
    {
        // 00:11:22:33:44:55 형태
        return System.Text.RegularExpressions.Regex.IsMatch(
            name,
            @"^([0-9A-Fa-f]{2}-){5}([0-9A-Fa-f]{2})$"
        );
    }
}
