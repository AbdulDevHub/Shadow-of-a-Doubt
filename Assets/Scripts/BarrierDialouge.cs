using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // For Keyboard & Mouse input (new Input System)

public class BarrierDialouge : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogueUI;   // Panel or dialogue box
    public TMP_Text dialogueText;   // Text field for message

    [Header("Dialogue Settings")]
    [TextArea(2, 5)]
    public string message = "I need to protect the lab!";

    private bool dialogueActive = false;
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered with: " + other.name);  // Add this line

        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            ShowDialogue();
        }
    }

    void ShowDialogue()
    {
        if (dialogueUI != null)
            dialogueUI.SetActive(true);

        if (dialogueText != null)
            dialogueText.text = message;

        dialogueActive = true;
    }

    void Update()
    {
        if (!dialogueActive) return;

        if (SkipPressed())
        {
            CloseDialogue();
        }
    }

    bool SkipPressed()
    {
        // Uses new Input System
        return (Keyboard.current != null &&
                ((Keyboard.current.enterKey != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
                 (Keyboard.current.numpadEnterKey != null && Keyboard.current.numpadEnterKey.wasPressedThisFrame)))
               ||
               (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    void CloseDialogue()
    {
        if (dialogueUI != null)
            dialogueUI.SetActive(false);

        dialogueActive = false;
    }
}
