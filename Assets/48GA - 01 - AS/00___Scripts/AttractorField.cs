using UnityEngine;

public class AttractorField : MonoBehaviour
{
    [Header("Text Settings")]
    public string textAmine = "Amine";
    public string text48 = "48";

    [Header("Gradient Spread")]
    [Range(1, 40)]
    public int blurPasses = 25;   

    public Texture2D TextureAmine { get; set; }
    public Texture2D Texture48 { get; set; }
    public Texture2D CurrentTexture { get; set; }

    public void SetTarget(bool useAmine)
    {
        CurrentTexture = useAmine ? TextureAmine : Texture48;
    }

    public Texture2D ApplySpread(Texture2D source, int passes)
    {
        int w = source.width;
        int h = source.height;

        Color[] pixels = source.GetPixels();
        Color[] result = new Color[pixels.Length];

        for (int pass = 0; pass < passes; pass++)
        {
            System.Array.Copy(pixels, result, pixels.Length);

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int i = y * w + x;
                    float max = 0f;
                    max = Mathf.Max(max, pixels[(y) * w + (x + 1)].r);
                    max = Mathf.Max(max, pixels[(y) * w + (x - 1)].r);
                    max = Mathf.Max(max, pixels[(y + 1) * w + (x)].r);
                    max = Mathf.Max(max, pixels[(y - 1) * w + (x)].r);
                    max = Mathf.Max(max, pixels[(y + 1) * w + (x + 1)].r);
                    max = Mathf.Max(max, pixels[(y - 1) * w + (x - 1)].r);
                    max = Mathf.Max(max, pixels[(y + 1) * w + (x - 1)].r);
                    max = Mathf.Max(max, pixels[(y - 1) * w + (x + 1)].r);

                    float spread = pixels[i].r;
                    if (max > spread)
                        spread = Mathf.Lerp(spread, max, 0.6f);

                    result[i] = new Color(spread, 0, 0, 1);
                }
            }
            System.Array.Copy(result, pixels, pixels.Length);
        }

        Texture2D output = new Texture2D(w, h, TextureFormat.RFloat, false);
        output.filterMode = FilterMode.Bilinear;
        output.wrapMode = TextureWrapMode.Clamp;
        output.SetPixels(pixels);
        output.Apply();
        return output;
    }
}