using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // New Input System

[System.Serializable]
public class DialogueLine
{
    public Sprite characterIcon;
    public string characterName;
    [TextArea(2, 5)]
    public string dialogueText;
    public bool useTypingEffect = true; // Toggle typing per line
}

public class DialogueSequence : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogueUI;
    public Image characterIconImage;
    public TMP_Text characterNameText;
    public TMP_Text dialogueText;

    [Header("Fade")]
    public Image fadePanel;
    public float fadeDuration = 1f;

    [Header("Dialogue Data")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
    public float typingSpeed = 0.03f; // Typing delay per character

    [Header("End Panel")]
    public bool hasEndPanel;
    public GameObject endPanel;
    public EndPanelController endPanelController;
    public float endPanelDelay = 1f; // pause before showing panel

    [Header("Score Example")]
    public int score = 123; // Replace with your real score variable

    void Start()
    {
        // Start fully black
        dialogueUI.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        fadePanel.color = new Color(0, 0, 0, 1);
        StartCoroutine(RunSceneSequence());
    }

    IEnumerator RunSceneSequence()
    {
        // Step 1: Fade in from black
        yield return StartCoroutine(Fade(1, 0));

        // Step 2: Show dialogue
        dialogueUI.SetActive(true);
        yield return StartCoroutine(RunDialogue());

        // Step 3: Hide dialogue
        dialogueUI.SetActive(false);

        if (hasEndPanel)
        {   
            //  and fade to 75% black
            yield return StartCoroutine(Fade(0, 0.75f));

            // Step 4: Pause before showing EndPanel
            yield return new WaitForSeconds(endPanelDelay);

            // Step 5: Show EndPanel
            if (endPanel != null)
            {
                endPanel.SetActive(true);

                // Show cursor for UI buttons
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                if (endPanelController != null)
                    endPanelController.SetScore(score);
            }
        }
            
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, to);
    }

    IEnumerator RunDialogue()
    {
        foreach (var line in dialogueLines)
        {
            characterIconImage.sprite = line.characterIcon;
            characterNameText.text = line.characterName;

            if (line.useTypingEffect)
            {
                dialogueText.text = "";
                int charIndex = 0;

                while (charIndex < line.dialogueText.Length)
                {
                    // Check if player wants to skip typing
                    if (SkipPressed())
                    {
                        dialogueText.text = line.dialogueText;

                        // Wait until input released before continuing
                        yield return new WaitWhile(SkipPressed);
                        break;
                    }

                    dialogueText.text += line.dialogueText[charIndex];
                    charIndex++;

                    yield return new WaitForSeconds(typingSpeed);
                }
            }
            else
            {
                // Instant text
                dialogueText.text = line.dialogueText;
            }

            // Wait for next click/press to continue
            yield return new WaitUntil(SkipPressed);

            // Prevent holding down from skipping multiple lines
            yield return new WaitWhile(SkipPressed);
        }
    }

    // Helper method for input detection
    bool SkipPressed()
    {
        return (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) ||
               (Mouse.current != null && Mouse.current.leftButton.isPressed);
    }
}
