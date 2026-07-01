using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static string SceneName;

    public static void LoadScene(string sceneName)
    {
        SceneName = sceneName;
        SceneManager.LoadScene("Loading");
    }
}
