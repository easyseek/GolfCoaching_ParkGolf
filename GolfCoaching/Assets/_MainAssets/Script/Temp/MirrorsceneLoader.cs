using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MirrorsceneLoader : MonoBehaviour
{
    public GameObject loadingScreen; // 로딩 화면 UI
    public Image progressBarImage; // 로딩 진행을 표시할 이미지
    public TextMeshProUGUI progressText; // 로딩 퍼센트를 표시할 텍스트 UI


    // 씬을 로드하는 메서드
    public void LoadMirrorScene()
    {
        StartCoroutine(LoadSceneAsync("MirrorMode"));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 로딩 화면을 활성화
        loadingScreen.SetActive(true);

        // 씬을 비동기로 로드
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // 로딩이 완료될 때까지 계속 진행
        while (!operation.isDone)
        {
            // 로딩 진행 상태를 이미지의 fillAmount에 반영
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // 0.9f로 나누는 이유는 로딩이 90%에서 완료로 표시되기 때문
            progressBarImage.fillAmount = progress;

            // 퍼센트 텍스트 업데이트 (TMP 사용)
            progressText.text = (progress * 100).ToString("F0") + "%";
            
            yield return null; // 다음 프레임까지 대기
        }
    }
}
