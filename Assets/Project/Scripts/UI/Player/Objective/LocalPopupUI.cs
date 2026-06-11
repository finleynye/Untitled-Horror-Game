using System.Collections;
using TMPro;
using UnityEngine;

public class LocalPopupUI : MonoBehaviour
{
    public static LocalPopupUI Instance;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Timing")]
    [SerializeField] private float fadeInSpeed = 4f;
    [SerializeField] private float holdTime = 3f;
    [SerializeField] private float fadeOutSpeed = 2f;

    private Coroutine popupRoutine;

    private void Awake()
    {
        Instance = this;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    public void ShowPopup(string locationMessage, string objectiveMessage)
    {
        if (canvasGroup == null) return;
        if (locationText == null) return;
        if (objectiveText == null) return;

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(ShowPopupRoutine(locationMessage, objectiveMessage));
    }

    private IEnumerator ShowPopupRoutine(string locationMessage, string objectiveMessage)
    {
        locationText.text = locationMessage;
        objectiveText.text = objectiveMessage;

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeInSpeed;
            yield return null;
        }

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdTime);

        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeOutSpeed;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }
}