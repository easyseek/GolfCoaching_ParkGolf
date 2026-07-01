using QRCoder;
using UnityEngine;

public static class QrCodeTextureGenerator
{
    public static Texture2D Generate(string text, int pixelsPerModule = 8, int quietZoneModules = 1)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new System.ArgumentException("QR text is empty.", nameof(text));

        using (QRCodeData qrCodeData = QRCodeGenerator.GenerateQrCode(text, QRCodeGenerator.ECCLevel.Q))
        {
            int moduleSize = qrCodeData.ModuleMatrix.Count;
            int moduleCount = moduleSize + quietZoneModules * 2;
            int textureSize = moduleCount * pixelsPerModule;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32 white = new Color32(255, 255, 255, 255);
            Color32 black = new Color32(0, 0, 0, 255);
            Color32[] pixels = new Color32[textureSize * textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                int moduleY = moduleSize - 1 - (y / pixelsPerModule - quietZoneModules);
                for (int x = 0; x < textureSize; x++)
                {
                    int moduleX = x / pixelsPerModule - quietZoneModules;
                    bool isDark = moduleX >= 0
                        && moduleX < moduleSize
                        && moduleY >= 0
                        && moduleY < moduleSize
                        && qrCodeData.ModuleMatrix[moduleY][moduleX];

                    pixels[y * textureSize + x] = isDark ? black : white;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
