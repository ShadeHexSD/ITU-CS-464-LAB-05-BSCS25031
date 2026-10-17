using UnityEngine;

public class Coin : MonoBehaviour
{
    public enum CoinKind { Normal, Red, Blue }

    [SerializeField] CoinKind kind = CoinKind.Normal;
    [SerializeField] int value = 1;
    [SerializeField] float spinSpeed = 90f;

    bool collected = false;

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;

        collected = true; // Prevent the same coin from registering twice.

        switch (kind)
        {
            case CoinKind.Normal:
                GameManager.Instance.AddScore(value);
                break;

            case CoinKind.Red:
                GameManager.Instance.CollectRedCoin();
                break;

            case CoinKind.Blue:
                GameManager.Instance.CollectBlueCoin();
                break;
        }

        Destroy(gameObject);
    }
}
