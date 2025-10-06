using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public Button difficultyButton;

    void Start()
    {
        if (difficultyButton != null)
            difficultyButton.onClick.AddListener(ChangeDifficulty);

        UpdateDifficultyButtonText();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Tutorial"); 
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void ChangeDifficulty()
    {
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
