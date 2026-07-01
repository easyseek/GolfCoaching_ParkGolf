using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingDirector : MonoBehaviour
{
    //[SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI m_LoadingText;

    private void Start()
    {
        StartCoroutine(LoadAsyncScene());
    }

    private IEnumerator LoadAsyncScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneLoader.SceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            //float progress = Mathf.Clamp01(op.progress / 0.9f);
            //if (loadingBar != null)
            //    loadingBar.value = progress;

            if (op.progress >= 0.9f)
            {
                yield return new WaitForSeconds(1.0f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
