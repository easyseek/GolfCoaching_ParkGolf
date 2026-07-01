using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProSearchDirector : MonoBehaviour
{
    //뒤로가기 버튼
    [SerializeField]
    private Button BackButton;

    //모두 버튼
    [SerializeField]
    private Toggle ToggleAll;

    private void Awake()
    {
        BackButton.onClick.AddListener(call: () =>
        {
            SceneManager.LoadScene(sceneName: "ProSelect");
        });
    }
}
