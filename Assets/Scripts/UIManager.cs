using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text scoreText;
    public Text bitesText;
    public Button eatButton;
    public GameObject gameOverPanel;
    public Text finalScoreText;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
            GameManager.Instance.OnEatModeToggled.AddListener(UpdateEatModeUI);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
            
            // Initial UI setup
            UpdateScoreUI();
            UpdateEatModeUI();
            
            if (bitesText != null) bitesText.gameObject.SetActive(false); // Hide obsolete bites text
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        if (eatButton != null)
        {
            eatButton.onClick.AddListener(OnEatButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(UpdateScoreUI);
            GameManager.Instance.OnEatModeToggled.RemoveListener(UpdateEatModeUI);
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



    private void UpdateEatModeUI()
    {
        if (eatButton != null && GameManager.Instance != null)
        {
            eatButton.interactable = true; // Always interactable, penalty evaluated on click
            
            Text btnText = eatButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = GameManager.Instance.IsEatMode ? "Cancel Eat" : "Eat (Cost: 2x)";
                btnText.color = GameManager.Instance.IsEatMode ? Color.red : Color.black;
            }
            else
            {
                // Fallback for newer Unity versions that default to TextMeshPro Buttons
                TMPro.TextMeshProUGUI tmpText = eatButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = GameManager.Instance.IsEatMode ? "Cancel Eat" : "Eat (Cost: 2x)";
                    tmpText.color = GameManager.Instance.IsEatMode ? Color.red : Color.black;
                }
            }
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

    private void OnEatButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleEatMode();
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
