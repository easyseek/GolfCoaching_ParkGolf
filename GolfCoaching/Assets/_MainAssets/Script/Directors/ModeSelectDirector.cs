using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectDirector : MonoBehaviour
{
    private const string SceneStretching = "Stretching";
    private const string SceneLesson = "LessonMode";
    private const string ScenePractice = "Mirror";
    private const string SceneAISwing = "AICoaching";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void OnClick_Stretching()
    {
        LoadScene(SceneStretching);
    }

    public void OnClick_LessonMode()
    {
        LoadScene(SceneLesson);
    }

    public void OnClick_Practice()
    {
        LoadScene(ScenePractice);
    }

    public void OnClick_AISwing()
    {
        LoadScene(SceneAISwing);
    }

    private void LoadScene(string sceneName)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectedSceneName = sceneName;
        }

        SceneManager.LoadScene(sceneName);
    }
}
