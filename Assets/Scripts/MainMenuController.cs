using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Tutorial"); // Replace with your first scene name
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game"); // Will show in editor
    }
}
