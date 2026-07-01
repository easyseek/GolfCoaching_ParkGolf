using UnityEngine;
using System.IO.Ports;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Linq;
using Enums;


public class LaunchManager : MonoBehaviourSingleton<LaunchManager>
{
    //dialout그룹 권한 필요.  > sudo usermod -aG dialout $USER 
    public string[] byIdNameKeywords = { "STMicroelectronics", "usb" };
    SerialPort stream;

    public string portName = "/dev/tty.usbserial-";
    public int baudRate = 115200;


    //bool isWait = false;

    CommandValue commandValue;
    RspData rspData;
    //bool isOk = true;
    //bool oneShot = false;

    public MonitorStatus monitorStatus = MonitorStatus.OFFLINE;

    public Action<RspData> OnDataSend;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Connect();
    }

    void Update()
    {
        if(stream != null && stream.IsOpen)
        {
            if(stream.BytesToRead > 0)
            {
                try
                {
                    string data = stream.ReadLine();
                    if(!string.IsNullOrEmpty(data))
                    {
                        
                        Debug.Log($"RSV DATA:{data}");

                        DataProcess(data);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"RSV ERR:{e.Message}");
                }
            }
        }
    }


    public bool IsConnected()
    {
        //return stream.IsOpen;
        return stream != null && stream.IsOpen;
    }

    public void Connect()
    {
        if (stream != null && stream.IsOpen)
        {
            stream.Close();
            //txtBtnConnect.text = "CONNECT";
            //Log("Port Disconnect.");
        }
        else
        {
            //portName = ipPortName.text;
            portName = FindTargetPort();//"/dev/ttyACM0";
            if(portName == null)
                portName = "/dev/ttyACM0";
            baudRate = 115200;//int.Parse(ipBaudRate.text.Trim());

            stream = new SerialPort(portName, baudRate);

            stream.ReadTimeout = 50;

            try
            {
                stream.DtrEnable = true;
                stream.RtsEnable = true;

                stream.Open();
                if (stream.IsOpen)
                {
                    //btnStart.interactable = true;
                    //btnStop.interactable = false;
                    //btnOneTIme.interactable = true;

                    PlayerPrefs.SetString("portName", portName);
                    PlayerPrefs.SetInt("baudRate", baudRate);


                    Debug.Log($"Port Connect Success! : {portName}, {baudRate}");

                    monitorStatus = MonitorStatus.STOP;

                }
            }
            catch (Exception e)
            {
                Debug.Log("Connect Failed:" + e.Message);

                PlayerPrefs.DeleteKey("portName");
                PlayerPrefs.SetInt("baudRate", 115200);
            }

            //txtBtnConnect.text = "DISCONNECT";
        }
    }


    private string FindTargetPort()
    {
        const string byIdDir = "/dev/serial/by-id";

        if (!Directory.Exists(byIdDir))
        {
            Debug.Log($"FindTargetPort() NotExists {byIdDir}");
            return null;
        }

        // by-id 아래의 항목 중 키워드가 모두 포함되는 파일 선택
        var candidates = Directory.GetFiles(byIdDir)
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return byIdNameKeywords.All(k =>
                    name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
            })
            .ToList();

        if (candidates.Count == 0)
            return null;

        // 첫 번째 후보 사용(복수 장치면 여기서 우선순위 로직을 더 넣으면 됨)
        var byIdPath = candidates[0];
        /*
        try
        {
            // 심볼릭 링크가 가리키는 실제 장치 파일(/dev/ttyACM0)을 resolve
            var resolved = ResolveSymlinkToAbsolute(byIdPath);
            if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
                return resolved;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SERIAL] Failed to resolve by-id link: {e.Message}");
        }
        */
        Debug.Log($"FindTargetPort() -> {byIdPath}");
        return byIdPath;//null;
    }



    void DataProcess(string data)
    {
        Debug.Log($"LaunchManager::DataProcess({data})");
        if(data.Contains("CMD"))
        {
            try
            {
                commandValue = JsonUtility.FromJson<CommandValue>(data);
            } 
            catch
            {
                commandValue = null;
            }

            if(commandValue != null )
            {
                if (commandValue.RSP.Equals("OK"))
                    monitorStatus = MonitorStatus.OK;//isOk = true;
                else
                    monitorStatus = MonitorStatus.FAILED;//isOk = false;

                //isWait = false;
            }
        }
        else if (data.Contains("BALL"))
        {
            try
            {
                rspData = JsonUtility.FromJson<RspData>(data);
            }
            catch
            {
                rspData = null;
            }

            if (rspData != null)
            {

                if(OnDataSend != null)
                    OnDataSend.Invoke(rspData);
                    
                /*
                if (float.TryParse(rspData.BALL, out float ball))
                    txtBall.text = $"{ball:F1} m/s     {(ball * 3.6f):F1} km/h";
                else
                    txtBall.text = "";

                if (float.TryParse(rspData.CLUB, out float club))
                    txtHead.text = $"{club:F1} m/s     {(club * 3.6f):F1} km/h";
                else
                    txtHead.text = "";

                if (int.TryParse(rspData.CARRY, out int carry))
                    txtCarry.text = $"{carry} m";
                else
                    txtCarry.text = "";

                if (int.TryParse(rspData.TOTAL, out int total))
                    txtTotal.text = $"{total} m";
                else
                    txtTotal.text = "";

                if (float.TryParse(rspData.ANGLE, out float angle))
                {
                    if(angle > 0)
                        txtDir.text = $"R {angle:F1}";
                    else
                        txtDir.text = $"L {-angle:F1}";
                }
                else
                    txtDir.text = "";
                */

                if(monitorStatus == MonitorStatus.MEASURE)
                {
                    monitorStatus = MonitorStatus.STOP;
                    //oneShot = false;
                    //btnStart.interactable = true;
                    //btnStop.interactable = false;
                    //btnOneTIme.interactable = true;
                }
            }
        }
    }

    public void MonitorStart()
    {
        StartCoroutine(cmdProcess(
            () =>
            {
                //btnStart.interactable = false;
                //btnStop.interactable = false;
                //btnOneTIme.interactable = false;

                CommandValue cmd = new CommandValue();
                cmd.CMD = "start";
                SendCommand(JsonUtility.ToJson(cmd));
            },
            (ret) =>
            {
                //btnStart.interactable = false;
                //btnStop.interactable = true;
                //btnOneTIme.interactable = false;
                if(ret)
                    monitorStatus = MonitorStatus.START;
                else
                    monitorStatus = MonitorStatus.STOP;
            }));
        
    }

    public void MonitorStop()
    {
        StartCoroutine(cmdProcess(
            () =>
            {
                //btnStart.interactable = false;
                //btnStop.interactable = false;
                //btnOneTIme.interactable = false;

                CommandValue cmd = new CommandValue();
                cmd.CMD = "stop";
                SendCommand(JsonUtility.ToJson(cmd));
            },
            (ret) =>
            {
                //btnStart.interactable = true;
                //btnStop.interactable = false;
                //btnOneTIme.interactable = true;

                monitorStatus = MonitorStatus.STOP;
            }));
    }

    public void MonitorOneTime()
    {
        StartCoroutine(cmdProcess(
            () =>
            {
                //btnStart.interactable = false;
                //btnStop.interactable = false;
                //btnOneTIme.interactable = false;

                CommandValue cmd = new CommandValue();
                cmd.CMD = "measure";
                SendCommand(JsonUtility.ToJson(cmd));
            },
            (ret) =>
            {
                //btnStart.interactable = false;
                //btnStop.interactable = true;
                //btnOneTIme.interactable = false;
                //oneShot = true;
                if(ret)
                    monitorStatus = MonitorStatus.MEASURE;
                else
                    monitorStatus = MonitorStatus.STOP;

            }));
    }

    IEnumerator cmdProcess(Action action, Action<bool> result)
    {
        action.Invoke();
        //isWait = true;
        monitorStatus = MonitorStatus.WAIT;

        yield return new WaitUntil(() => (monitorStatus == MonitorStatus.OK || monitorStatus == MonitorStatus.FAILED));//isWait == false);

        result.Invoke(monitorStatus == MonitorStatus.OK ? true : false);
    }

    public void SendCommand(string cmd)
    {
        if(stream.IsOpen)
        {
            try
            {
                //stream.Write(ipCommand.text + "\n");
                Debug.Log($"SendCommand() > {cmd}");
                stream.Write(cmd + "\n");
                
            }
            catch (Exception e)
            {
                Debug.Log($"Send Err : {e.Message}");
            }
        }
    }

    void OlicationQuit()
    {
        if(stream != null && stream.IsOpen)
        {
            monitorStatus = MonitorStatus.OFFLINE;
            stream.Close();
        }
    }









}


[Serializable]
public class CommandValue
{
    public string CMD;
    public string RSP;
    public int VALUE;
}

[Serializable]
public class RspData
{
    public string BALL; //m/s
    public string CLUB; //m/s
    public string TOTAL;//m
    public string CARRY;//m
    public string ANGLE;//degree
}