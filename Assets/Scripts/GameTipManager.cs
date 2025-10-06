using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class GameTipManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI textObject; // Assign your TMP text object here

    [Header("Text Settings")]
    [TextArea(2, 5)]
    public string[] messages; // Add all your text lines here in the Inspector

    [Tooltip("Time (in seconds) before switching to the next message")]
    public float cycleInterval = 5f;

    [Header("Fade Settings")]
    [Tooltip("Duration of the fade-in and fade-out animations")]
    public float fadeDuration = 1f;

    private int currentIndex = 0;
    private Coroutine cycleRoutine;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (textObject == null)
        {
            Debug.LogError("TextCyclerWithFade: No TextMeshProUGUI object assigned!");
            enabled = false;
            return;
        }

        if (messages.Length == 0)
        {
            Debug.LogWarning("TextCyclerWithFade: No messages provided.");
            return;
        }

        textObject.text = messages[currentIndex];
        canvasGroup.alpha = 1f;

        cycleRoutine = StartCoroutine(CycleText());
    }

    private IEnumerator CycleText()
    {
        while (true)
        {
            yield return new WaitForSeconds(cycleInterval);

            // Fade out
            yield return StartCoroutine(FadeTo(0f));

            // Change text
            currentIndex = (currentIndex + 1) % messages.Length;
            textObject.text = messages[currentIndex];

            // Fade in
            yield return StartCoroutine(FadeTo(1f));
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void OnDisable()
    {
        if (cycleRoutine != null)
            StopCoroutine(cycleRoutine);
    }
}
