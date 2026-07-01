using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class PracticeModeVideoDirector : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    private bool _isPrepared = false;
    private bool _isPlay = false;

    private void Start()
    {
        videoPlayer.isLooping = false;
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        _isPrepared = true;
        videoPlayer.Play();
        _isPlay = true;
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        videoPlayer.Stop();
        videoPlayer.time = 0;
        _isPlay = false;
    }

    public void OnClick_Video()
    {
        if (!_isPrepared) return;

        if (_isPlay)
        {
            videoPlayer.Pause();
            _isPlay = false;
        }
        else
        {
            videoPlayer.Play();
            _isPlay = true;
        }
    }

    public void PlayVideo(string url)
    {
        _isPrepared = false;
        _isPlay = false;
        videoPlayer.url = url;
        videoPlayer.Prepare();
    }

    public void OnClick_Home()
    {
        GameManager.Instance.SelectedSceneName = string.Empty;
        SceneManager.LoadScene("ModeSelect");
    }
}
