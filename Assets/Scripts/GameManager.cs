using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; }
    public bool IsEatMode { get; private set; }
    public bool IsGameOver { get; private set; }

    public Dictionary<string, int> ItemsOnBoard { get; private set; } = new Dictionary<string, int>();

    public UnityEvent OnScoreChanged;
    public UnityEvent OnEatModeToggled;
    public UnityEvent OnGameOver;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Score = 0;
            IsEatMode = false;
            IsGameOver = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        OnScoreChanged?.Invoke();
    }

    public void AddScore(int amount)
    {
        if (IsGameOver) return;
        Score += amount;
        if (Score < 0) Score = 0; // Prevent negative score
        OnScoreChanged?.Invoke();
    }

    public void RegisterItem(FoodItem item)
    {
        string name = item.itemName;
        int currentCount = 0;
        
        if (ItemsOnBoard.TryGetValue(name, out int count))
        {
            currentCount = count;
            ItemsOnBoard[name] = count + 1;
        }
        else
        {
            ItemsOnBoard[name] = 1;
        }

        // Formula: BaseScore * (1 + (Count on board * 0.5))
        float multiplier = 1f + (currentCount * 0.5f);
        int pointsAwarded = Mathf.RoundToInt(item.baseScore * multiplier);
        
        AddScore(pointsAwarded);
    }

    public void UnregisterItem(FoodItem item)
    {
        string name = item.itemName;
        if (ItemsOnBoard.ContainsKey(name))
        {
            ItemsOnBoard[name]--;
            if (ItemsOnBoard[name] <= 0)
            {
                ItemsOnBoard.Remove(name);
            }
        }
    }

    public int GetEatPenalty(FoodItem item)
    {
        int count = 0;
        ItemsOnBoard.TryGetValue(item.itemName, out count);
        
        // Cost is 2x what the item is currently worth
        float multiplier = 1f + (Mathf.Max(0, count - 1) * 0.5f);
        int currentWorth = Mathf.RoundToInt(item.baseScore * multiplier);
        return currentWorth * 2;
    }

    public void ToggleEatMode()
    {
        Debug.Log("GameManager: ToggleEatMode called! Current: " + IsEatMode + ", GameOver: " + IsGameOver);
        if (IsGameOver) return;

        IsEatMode = !IsEatMode;
        Debug.Log("GameManager: EatMode is now " + IsEatMode);
        OnEatModeToggled?.Invoke();
    }

    public bool TryConsumeItem(FoodItem item)
    {
        int penalty = GetEatPenalty(item);
        if (Score >= penalty)
        {
            AddScore(-penalty);
            IsEatMode = false; // Turn off eat mode after eating
            OnEatModeToggled?.Invoke();
            return true;
        }
        return false;
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        
        IsGameOver = true;
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        // Reload current scene for fast restart
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
