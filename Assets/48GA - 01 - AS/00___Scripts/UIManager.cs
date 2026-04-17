using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Overlay")]
    public CanvasGroup overlayGroup;
    public TMPro.TextMeshProUGUI overlayText;

    [Header("References")]
    public PhaseController phaseController;

    [Header("Fade Settings")]
    public float fadeDuration = 1.2f;
    public float visibleTime = 4.0f;

    void Start()
    {
        if (overlayGroup != null)
            overlayGroup.alpha = 0f;

        phaseController.OnConvergingStarted += (isAmine) =>
        {
            if (!isAmine)
                StartCoroutine(ShowOverlay(""));
        };

        phaseController.OnCollapseStarted += () =>
        {
            if (overlayGroup != null)
                StartCoroutine(FadeOut(overlayGroup, 0.5f));
        };
    }

    IEnumerator ShowOverlay(string message)
    {
        if (overlayText != null) overlayText.text = message;
        yield return StartCoroutine(FadeIn(overlayGroup, fadeDuration));
        yield return new WaitForSeconds(visibleTime);
        yield return StartCoroutine(FadeOut(overlayGroup, fadeDuration));
    }

    IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        float t = 0f;
        while (t < duration) { t += Time.deltaTime; cg.alpha = t / duration; yield return null; }
        cg.alpha = 1f;
    }

    IEnumerator FadeOut(CanvasGroup cg, float duration)
    {
        float t = 0f; float start = cg.alpha;
        while (t < duration) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(start, 0f, t / duration); yield return null; }
        cg.alpha = 0f;
    }
}