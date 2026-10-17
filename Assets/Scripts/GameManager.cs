using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] int targetScore = 10;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject winPanel;

    int score;

    void Awake() => Instance = this;

    void Start() => UpdateUI();

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();

        if (score >= targetScore)
            Win();
    }

    void UpdateUI()
    {
        scoreText.text = $"Score: {score} / {targetScore}";
    }

    void Win()
    {
        winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
