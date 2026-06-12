using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance;

    [Header("Fade References")]
    [SerializeField] private Image fadeImage;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInTime = 1f;
    [SerializeField] private float fadeOutTime = 0.75f;
    [SerializeField] private Color fadeColour = Color.black;

    [Header("Start Settings")]
    [SerializeField] private bool fadeInOnStart = true;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);

            Color colour = fadeColour;
            colour.a = 1f;
            fadeImage.color = colour;
        }
    }

    private void Start()
    {
        if (fadeInOnStart)
            FadeIn();
    }

    public void FadeIn()
    {
        StartFade(1f, 0f, fadeInTime, null);
    }

    public void FadeOut()
    {
        StartFade(0f, 1f, fadeOutTime, null);
    }
    public void FadeOutThenRun(UnityEvent eventToRun)
    {
        StartFade(0f, 1f, fadeOutTime, () =>
        {
            eventToRun?.Invoke();
        });
    }
    private void StartFade(float startAlpha, float targetAlpha, float duration, UnityAction onFadeComplete)
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("ScreenFade is missing fadeImage.");
            onFadeComplete?.Invoke();
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(startAlpha, targetAlpha, duration, onFadeComplete));
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration, UnityAction onFadeComplete)
    {
        fadeImage.gameObject.SetActive(true);

        Color colour = fadeColour;
        colour.a = startAlpha;
        fadeImage.color = colour;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = timer / duration;
            colour.a = Mathf.Lerp(startAlpha, targetAlpha, progress);

            fadeImage.color = colour;

            yield return null;
        }

        colour.a = targetAlpha;
        fadeImage.color = colour;

        if (targetAlpha <= 0f)
            fadeImage.gameObject.SetActive(false);

        onFadeComplete?.Invoke();
    }
}