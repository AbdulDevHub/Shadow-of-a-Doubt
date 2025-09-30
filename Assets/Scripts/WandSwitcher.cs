using UnityEngine;
using UnityEngine.InputSystem; // <-- IMPORTANT

public class WandSwitcher : MonoBehaviour
{
    public GameObject[] wands;
    private int currentIndex = 0;

    void Start()
    {
        SelectWand(0);
    }

    void Update()
    {
        var keyboard = Keyboard.current;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SelectWand(0);

        if (keyboard.digit2Key.wasPressedThisFrame)
            SelectWand(1);

        if (keyboard.digit3Key.wasPressedThisFrame)
            SelectWand(2);
    }

    void SelectWand(int index)
    {
        if (index < 0 || index >= wands.Length) return;

        currentIndex = index;
        for (int i = 0; i < wands.Length; i++)
        {
            wands[i].SetActive(i == index);
        }
    }
}
