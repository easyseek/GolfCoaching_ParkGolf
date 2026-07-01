using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayCard : MonoBehaviour
{
    [SerializeField] RawImage rawThumbnail;
    [SerializeField] TextMeshProUGUI txtTime;
    string frontPath;
    string sidePath;
    Action<string,string> ActionPlay;

    public void SetReplayCard(Texture2D image, string time, string front, string side, Action<string, string> act)
    {
        rawThumbnail.texture = image;
        txtTime.text = time;
        frontPath = front;
        sidePath = side;
        ActionPlay = act;
    }

    public void OnClick_Play()
    {
        //Debug.Log("OnClick_Play()");
        if (ActionPlay != null)
        {
            ActionPlay.Invoke(frontPath, sidePath);
        }
    }
}
