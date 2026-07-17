using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text scoreText;
    public Text bitesText;
    public GameObject gameOverPanel;
    public Text finalScoreText;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
            
            // Initial UI setup
            UpdateScoreUI();
            
            if (bitesText != null) bitesText.gameObject.SetActive(false); // Hide obsolete bites text
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(UpdateScoreUI);
            GameManager.Instance.OnGameOver.RemoveListener(ShowGameOver);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null && GameManager.Instance != null)
        {
            scoreText.text = "Score: " + GameManager.Instance.Score.ToString();
        }
    }


    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null && GameManager.Instance != null)
        {
            finalScoreText.text = "Final Score: " + GameManager.Instance.Score.ToString();
        }
    }


    public void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }
}
