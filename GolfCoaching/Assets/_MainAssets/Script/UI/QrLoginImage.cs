using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class QrLoginImage : MonoBehaviour
{
    [SerializeField] private int pixelsPerModule = 8;
    [SerializeField] private int quietZoneModules = 1;
    [SerializeField] private TextMeshProUGUI debugUrlText = null;

    private Image targetImage;
    private Sprite generatedSprite;
    private Texture2D generatedTexture;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    public void SetLoginUrl(string loginUrl)
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (string.IsNullOrWhiteSpace(loginUrl))
        {
            Clear();
            return;
        }

        DestroyGeneratedAssets();

        generatedTexture = QrCodeTextureGenerator.Generate(loginUrl, pixelsPerModule, quietZoneModules);
        generatedSprite = Sprite.Create(
            generatedTexture,
            new Rect(0, 0, generatedTexture.width, generatedTexture.height),
            new Vector2(0.5f, 0.5f),
            generatedTexture.width);

        targetImage.sprite = generatedSprite;
        targetImage.preserveAspect = true;
        gameObject.SetActive(true);

        if (debugUrlText != null)
            debugUrlText.text = loginUrl;
    }

    public void Clear()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        targetImage.sprite = null;
        if (debugUrlText != null)
            debugUrlText.text = string.Empty;

        DestroyGeneratedAssets();
    }

    private void OnDestroy()
    {
        DestroyGeneratedAssets();
    }

    private void DestroyGeneratedAssets()
    {
        if (generatedSprite != null)
        {
            Destroy(generatedSprite);
            generatedSprite = null;
        }

        if (generatedTexture != null)
        {
            Destroy(generatedTexture);
            generatedTexture = null;
        }
    }
}
