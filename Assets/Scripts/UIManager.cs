using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI messageText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        messageText.text = ""; // start empty
    }

    public void ShowMessage(string msg)
    {
        messageText.text = msg;
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 2f); // clear after 2 seconds
    }

    private void ClearMessage()
    {
        messageText.text = "";
    }
}
