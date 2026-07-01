using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class RangeDirector : MonoBehaviour
{
    private void Start()
    {
        Init();
    }

    private void Init()
    {
        GameManager.Instance.SetOptionPanel();
    }

    public void Onclick_Button()
    {
        GameObject obj = EventSystem.current.currentSelectedGameObject;

        switch (obj.name)
        {
            case "Home":
                GameManager.Instance.SelectedSceneName = string.Empty;
                SceneManager.LoadScene("ModeSelect");
                break;

            case "Option":
                GameManager.Instance.OnClick_OptionPanel();
                break;
        }
    }
}
