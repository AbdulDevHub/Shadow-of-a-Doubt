using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GhostKillManager : MonoBehaviour
{
    public static GhostKillManager Instance;

    [Header("Kill Settings")]
    [Tooltip("How many ghost kills are required before fade out triggers.")]
    public int requiredKills = 5; 
    private int currentKills = 0;

    [Header("Fade Settings")]
    [SerializeField] private Image fadePanel;   // Drag FadePanel (full screen Image)
    [SerializeField] private float fadeDuration = 2f;

    [Header("Scene Transition")]
    [Tooltip("Leave blank to reload current scene.")]
    public string nextSceneName;

    [Header("Start Fade Options")]
    [Tooltip("If true, scene will start black and fade into gameplay.")]
    public bool fadeInOnStart = true;
    [Tooltip("Check if DialogueSequence is handling the initial fade (recommended).")]
    public bool dialogueHandlesFade = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Only handle initial fade if DialogueSequence isn't doing it
        if (!dialogueHandlesFade && fadePanel != null && fadeInOnStart)
        {
            // Start black, then fade in
            Color c = fadePanel.color;
            c.a = 1f;
            fadePanel.color = c;
            StartCoroutine(FadePanel(1f, 0f, fadeDuration, null));
        }
    }

    private bool levelEndingTriggered = false;
    public void RegisterKill()
    {
        if (levelEndingTriggered) return;

        currentKills++;

        if (currentKills >= requiredKills)
        {
            levelEndingTriggered = true;
            StartCoroutine(WaitForNoGhosts());
        }
    }

    private IEnumerator WaitForNoGhosts()
    {
        // ✅ Wait until all ghosts are truly gone
        while (FindObjectsOfType<GhostHealth>().Length > 0)
            yield return null;

        // ✅ Now safely trigger the fade + dialogue
        StartCoroutine(HandleLevelComplete());
    }

    private IEnumerator HandleLevelComplete()
    {
        // Optional: Disable combat UI so it doesn't overlap
        DialogueSequence dialogue = FindObjectOfType<DialogueSequence>();

        if (dialogue != null)
        {
            // 🔹 Tell DialogueSequence to play its outro
            yield return dialogue.PlayOutroDialogue();
        }

        // 🔹 After dialogue finishes, fade out and change scene
        yield return StartCoroutine(FadePanel(0f, 1f, fadeDuration, () =>
        {
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }));
    }

    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration, System.Action onComplete)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        Color c = fadePanel.color;
        c.a = startAlpha;
        fadePanel.color = c;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            fadePanel.color = c;
            yield return null;
        }

        c.a = endAlpha;
        fadePanel.color = c;

        onComplete?.Invoke();
    }
}