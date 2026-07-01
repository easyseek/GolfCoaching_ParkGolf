using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class OptionPanelController : MonoBehaviour
{
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] Slider sliderVolume;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Awake()
    {
        float volume = PlayerPrefs.GetFloat("DirectAudioVolume", 0.5f);
        videoPlayer.SetDirectAudioVolume(0, volume);
        sliderVolume.value = volume;
        sliderVolume.onValueChanged.AddListener(OnValueChanged_Volume);
    }

    protected virtual void OnClick_ModeChange(string sceneName)
    {
        if (string.IsNullOrEmpty(GameManager.Instance.SelectedSceneName) || GameManager.Instance.SelectedSceneName.Equals(sceneName))
            return;

        GameManager.Instance.SelectedSceneName = sceneName;
        //GameManager.Instance.Stance.Clear();
        GameManager.Instance.Club = Enums.EClub.None;
        SceneManager.LoadScene("Pose");
    }

    protected virtual void OnClick_PoseChange()
    {
        SceneManager.LoadScene("Pose");
    }

    protected virtual void OnClick_ClubChange()
    {
        SceneManager.LoadScene("ClubChange");
    }

    protected virtual void OnClick_SetCaption()
    {

    }

    protected virtual void OnClick_Bluetooth()
    {
        // Windows 설정 앱의 Bluetooth 페이지를 엽니다
        Application.OpenURL("ms-settings:bluetooth");
    }

    protected virtual void OnClick_Setting()
    {

    }

    protected virtual void OnValueChanged_Volume(float value)
    {
        if(videoPlayer != null)
        {
            videoPlayer.SetDirectAudioVolume(0, (float)value);
            PlayerPrefs.SetFloat("DirectAudioVolume", (float)value);
        }
    }
}
