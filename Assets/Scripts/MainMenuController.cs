using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public Button difficultyButton;

    [Header("Button Sounds")]
    [SerializeField] private AudioClip clickSound;   // For Play and Quit buttons
    [SerializeField] private AudioClip toggleSound;  // For difficulty button

    [Header("Menu Music")]
    [SerializeField] private AudioClip menuMusic;
    private AudioSource musicSource;

    void Start()
    {
        // Button listener
        if (difficultyButton != null)
            difficultyButton.onClick.AddListener(ChangeDifficulty);

        UpdateDifficultyButtonText();

        // ✅ Play menu music
        if (menuMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = menuMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.Play();
        }
    }

    public void PlayGame()
    {
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, Vector3.zero);

        if (musicSource != null)
            musicSource.Stop();

        SceneManager.LoadScene("Tutorial");
    }

    public void QuitGame()
    {
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, Vector3.zero);

        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void ChangeDifficulty()
    {
        if (toggleSound != null)
            AudioSource.PlayClipAtPoint(toggleSound, Vector3.zero);

        DifficultyManager.Instance.CycleDifficulty();
        UpdateDifficultyButtonText();
        Debug.Log("Difficulty changed to: " + DifficultyManager.Instance.CurrentDifficulty);
    }

    private void UpdateDifficultyButtonText()
    {
        if (difficultyButton != null)
        {
            TextMeshProUGUI btnText = difficultyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = "Difficulty: " + DifficultyManager.Instance.CurrentDifficulty;
        }
    }
}
