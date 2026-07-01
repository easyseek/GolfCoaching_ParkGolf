using System.IO;
using UnityEngine;

[RequireComponent(typeof(VLCVideoPlayer))]
public class VLCStreamingAssetsSource : MonoBehaviour
{
    [SerializeField] private string relativePath;

    private void Awake()
    {
        VLCVideoPlayer player = GetComponent<VLCVideoPlayer>();
        player.url = Path.Combine(Application.streamingAssetsPath, relativePath);
    }
}
