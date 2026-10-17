using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    const int TotalBonusCoins = 50;

    [Header("Score and UI")]
    [SerializeField] int targetScore = 10;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text bonusScoreText;
    [SerializeField] GameObject winPanel;

    [Header("Special Coins")]
    [SerializeField] GameObject redCoinPrefab;
    [SerializeField] GameObject blueCoinPrefab;
    [SerializeField] Transform player;

    [Header("Map Boundaries")]
    [SerializeField] Collider groundCollider;
    [SerializeField] float edgePadding = 0.6f;

    [Header("Spawn Settings")]
    [SerializeField] float redSpawnDistance = 4f;
    [SerializeField] float bonusRadius = 6f;
    [SerializeField] float coinHeight = 0.5f;
    [SerializeField] float redAtTenGraceSeconds = 7f;

    int score = 0;
    int bonusCoins = 0;
    int redSpawnAtScore;
    bool redSpawned = false;
    bool redCollected = false;
    bool waitingAtTen = false;
    bool won = false;

    void Awake()
    {
        Instance = this;
        redSpawnAtScore = Random.Range(1, Mathf.Max(1, targetScore) + 1);
    }

    void Start()
    {
        Time.timeScale = 1f;
        scoreText.text = $"Score: {score} / {targetScore}";
        scoreText.gameObject.SetActive(true);

        bonusScoreText.text = $"BonusCoins: {bonusCoins}/{TotalBonusCoins}";
        bonusScoreText.gameObject.SetActive(false);
        winPanel.SetActive(false);

        Debug.Log($"Red coin will appear at score {redSpawnAtScore}.");
    }

    // Yellow/normal coins: only count until the player chooses the red coin.
    public void AddScore(int amount)
    {
        if (won || redCollected) return;

        score = Mathf.Min(targetScore, score + amount);
        scoreText.text = $"Score: {score} / {targetScore}";

        if (!redSpawned && score >= redSpawnAtScore)
        {
            redSpawned = true;
            SpawnRedCoin();
        }

        if (score >= targetScore)
        {
            // If the random red coin appears exactly at 10, give the player
            // a few seconds to collect it before a normal 10/10 victory.
            if (redSpawnAtScore == targetScore && redSpawned)
            {
                if (!waitingAtTen)
                {
                    waitingAtTen = true;
                    Invoke(nameof(WinIfNoRed), redAtTenGraceSeconds);
                }
            }
            else
                Win();
        }
    }

    bool TryGetGround(out Bounds bounds)
    {
        bounds = new Bounds();

        if (groundCollider == null)
        {
            Debug.LogError("GameManager: Assign the Ground Collider in the Inspector.");
            return false;
        }

        bounds = groundCollider.bounds;
        if (bounds.extents.x <= edgePadding || bounds.extents.z <= edgePadding)
        {
            Debug.LogError("GameManager: Ground is too small for the chosen Edge Padding.");
            return false;
        }

        return true;
    }

    void SpawnRedCoin()
    {
        if (redCoinPrefab == null || player == null) return;
        if (!TryGetGround(out Bounds bounds)) return;

        float minX = bounds.min.x + edgePadding;
        float maxX = bounds.max.x - edgePadding;
        float minZ = bounds.min.z + edgePadding;
        float maxZ = bounds.max.z - edgePadding;

        Vector3 bestPosition = player.position;
        float bestDistance = -1f;

        // Try different directions so the red coin is near the player,
        // but always inside the map. Keep the farthest valid fallback.
        for (int attempt = 0; attempt < 40; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 position = player.position +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * redSpawnDistance;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
            position.y = bounds.max.y + coinHeight;

            float distance = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(player.position.x, player.position.z));

            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestPosition = position;
            }

            if (distance >= 2f) break;
        }

        Instantiate(redCoinPrefab, bestPosition, Quaternion.identity);
    }

    // Called only when the RED coin is collected.
    public void CollectRedCoin()
    {
        if (won || redCollected) return;
        if (blueCoinPrefab == null || player == null)
        {
            Debug.LogError("GameManager: Blue Coin Prefab or Player is missing.");
            return;
        }
        if (!TryGetGround(out Bounds bounds)) return;

        // Entire circular pattern must fit inside the rectangular floor.
        float radius = Mathf.Min(bonusRadius,
            Mathf.Min(bounds.extents.x, bounds.extents.z) - edgePadding);

        if (radius <= 0f) return;

        // Near a map edge, shift the circle center inward, rather than
        // placing blue coins outside the map or bunching them at the border.
        Vector3 center = player.position;
        center.x = Mathf.Clamp(center.x,
            bounds.min.x + radius + edgePadding,
            bounds.max.x - radius - edgePadding);
        center.z = Mathf.Clamp(center.z,
            bounds.min.z + radius + edgePadding,
            bounds.max.z - radius - edgePadding);

        redCollected = true;
        CancelInvoke(nameof(WinIfNoRed));

        // Switch from normal score mode to bonus-only mode.
        scoreText.gameObject.SetActive(false);
        bonusScoreText.text = $"BonusCoins: {bonusCoins}/{TotalBonusCoins}";
        bonusScoreText.gameObject.SetActive(true);

        int[] coinsPerRing = { 16, 17, 17 }; // Total = 50.

        for (int ring = 0; ring < coinsPerRing.Length; ring++)
        {
            int count = coinsPerRing[ring];
            float ringRadius = radius * (ring + 1) / coinsPerRing.Length;

            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                Vector3 position = center + new Vector3(
                    Mathf.Cos(angle) * ringRadius,
                    0f,
                    Mathf.Sin(angle) * ringRadius);

                position.y = bounds.max.y + coinHeight;
                Instantiate(blueCoinPrefab, position, Quaternion.identity);
            }
        }

        Debug.Log("Red coin collected: spawned 50 blue coins inside the map.");
    }

    public void CollectBlueCoin()
    {
        if (!redCollected || won || bonusCoins >= TotalBonusCoins) return;

        bonusCoins++;
        bonusScoreText.text = $"BonusCoins: {bonusCoins}/{TotalBonusCoins}";

        if (bonusCoins >= TotalBonusCoins)
            Win();
    }

    void WinIfNoRed()
    {
        if (!redCollected && score >= targetScore)
            Win();
    }

    void Win()
    {
        if (won) return;
        won = true;
        winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
