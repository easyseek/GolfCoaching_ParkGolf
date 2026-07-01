using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CaptureRenderTexture : MonoBehaviour
{
    // Inspector에서 연결해줄 RawImage
    public RawImage rawImage;

    // 캡처 후 저장될 파일 이름 (원하시는 대로 수정 가능)
    public string fileName = "CapturedImage.png";

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.P))
        {
            CaptureAndSaveImage();
        }
    }

    /// <summary>
    /// RenderTexture -> Texture2D -> PNG로 변환하여 파일로 저장하는 함수
    /// </summary>
    public void CaptureAndSaveImage()
    {
        // RawImage가 보여주고 있는 Texture가 RenderTexture라고 가정
        RenderTexture renderTex = rawImage.texture as RenderTexture;
        if (renderTex == null)
        {
            Debug.LogError("RawImage에 연결된 Texture가 RenderTexture가 아닙니다.");
            return;
        }

        // 현재 활성화되어 있는 RenderTexture 백업
        RenderTexture currentRT = RenderTexture.active;

        // 캡처할 RenderTexture를 활성화
        RenderTexture.active = renderTex;

        // RenderTexture의 크기에 맞춰 Texture2D 생성
        Texture2D captureTexture = new Texture2D(
            renderTex.width,
            renderTex.height,
            TextureFormat.RGB24,
            false
        );

        // RenderTexture의 픽셀을 읽어서 Texture2D에 적용
        captureTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        captureTexture.Apply();

        // 다시 원래 RenderTexture로 복구
        RenderTexture.active = currentRT;

        // Texture2D를 PNG 포맷으로 변환
        byte[] pngData = captureTexture.EncodeToPNG();

        // 파일로 저장할 경로 설정
        // 예: Application.persistentDataPath, Application.dataPath 등 원하는 경로 사용 가능
        string savePath = Path.Combine(Application.persistentDataPath, fileName);

        // PNG 데이터 파일로 저장
        File.WriteAllBytes(savePath, pngData);

        // 디버그 확인
        Debug.Log($"RenderTexture 캡처 완료! 저장 경로: {savePath}");
    }
}
