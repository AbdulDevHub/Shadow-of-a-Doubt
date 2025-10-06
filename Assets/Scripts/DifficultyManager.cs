using UnityEngine;

public enum Difficulty { Easy, Med, Hard }

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Easy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // persists across scenes
    }

    public void CycleDifficulty()
    {
        CurrentDifficulty = (Difficulty)(((int)CurrentDifficulty + 1) % 
            System.Enum.GetValues(typeof(Difficulty)).Length);
    }

    public (float min, float max) GetSpawnIntervals()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy: return (0.5f, 6f);
            case Difficulty.Med:  return (0.5f, 4f);
            case Difficulty.Hard: return (0.5f, 2f);
            default: return (0.5f, 6f);
        }
    }
}
