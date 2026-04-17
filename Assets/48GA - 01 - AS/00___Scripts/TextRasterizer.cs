using UnityEngine;
using System.Collections;

public class TextRasterizer : MonoBehaviour
{
    [Header("References")]
    public AttractorField attractorField;
    public ParticleSystemGPU myparticleSystem;

    [Header("Text Appearance")]
    public FontStyle fontStyle = FontStyle.Bold;
    public Color glowColor = Color.white;

    private RenderTexture rtAmine;
    private RenderTexture rt48;

    private bool renderingAmine = true;
    private bool pendingRender = false;
    private int renderFrame = 0;

    void Start()
    {
        int w = Screen.width;
        int h = Screen.height;

        rtAmine = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        rt48 = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        rtAmine.Create();
        rt48.Create();

        StartCoroutine(RasterizeBothTextures());
    }

    IEnumerator RasterizeBothTextures()
    {
        renderingAmine = true;
        pendingRender = true;
        renderFrame = 0;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Texture2D amineTex = ReadRenderTexture(rtAmine, Screen.width, Screen.height);
        amineTex = attractorField.ApplySpread(amineTex, attractorField.blurPasses);
        attractorField.TextureAmine = amineTex;

        renderingAmine = false;
        pendingRender = true;
        renderFrame = 0;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Texture2D tex48 = ReadRenderTexture(rt48, Screen.width, Screen.height);
        tex48 = attractorField.ApplySpread(tex48, attractorField.blurPasses);
        attractorField.Texture48 = tex48;

        attractorField.CurrentTexture = amineTex;
        pendingRender = false;

        if (myparticleSystem != null)
            myparticleSystem.OnAttractorTexturesReady();

        Debug.Log("[TextRasterizer] Texturas generadas!");
    }

    void OnGUI()
    {
        if (!pendingRender) return;
        renderFrame++;
        if (renderFrame < 2) return;

        string textToRender = renderingAmine ? attractorField.textAmine : attractorField.text48;
        RenderTexture target = renderingAmine ? rtAmine : rt48;

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = target;
        GL.Clear(true, true, Color.black);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontStyle = fontStyle;
        style.alignment = TextAnchor.MiddleCenter;

        int fs = renderingAmine
            ? Mathf.RoundToInt(Screen.width * 0.12f)
            : Mathf.RoundToInt(Screen.width * 0.16f);
        style.fontSize = fs;

        Matrix4x4 prevMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);

        Rect fullRect = new Rect(0, 0, Screen.width, Screen.height);

        int offset = Mathf.Max(3, fs / 25);
        style.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        GUI.color = Color.white;
        GUI.Label(new Rect(-offset, -offset, Screen.width, Screen.height), textToRender, style);
        GUI.Label(new Rect(offset, -offset, Screen.width, Screen.height), textToRender, style);
        GUI.Label(new Rect(-offset, offset, Screen.width, Screen.height), textToRender, style);
        GUI.Label(new Rect(offset, offset, Screen.width, Screen.height), textToRender, style);
        GUI.Label(new Rect(0, -offset, Screen.width, Screen.height), textToRender, style);
        GUI.Label(new Rect(0, offset, Screen.width, Screen.height), textToRender, style);
        GUI.Label(new Rect(-offset, 0, Screen.width, Screen.height), textToRender, style);
        GUI.Label(new Rect(offset, 0, Screen.width, Screen.height), textToRender, style);

        style.normal.textColor = glowColor;
        GUI.Label(fullRect, textToRender, style);

        GUI.matrix = prevMatrix;
        RenderTexture.active = prev;

        pendingRender = false;
    }

    Texture2D ReadRenderTexture(RenderTexture rt, int w, int h)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RFloat, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Texture2D tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tmp.Apply();

        Color[] srcPixels = tmp.GetPixels();
        Color[] dstPixels = new Color[w * h];
        for (int i = 0; i < srcPixels.Length; i++)
        {
            float brightness = srcPixels[i].r * 0.299f + srcPixels[i].g * 0.587f + srcPixels[i].b * 0.114f;
            dstPixels[i] = new Color(brightness, 0, 0, 1);
        }

        tex.SetPixels(dstPixels);
        tex.Apply();

        RenderTexture.active = prev;
        Destroy(tmp);
        return tex;
    }
}