using UnityEngine;
using System.Collections;

public class FoodItem : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Impact Wobble Settings")]
    [Range(0f, 2f)]
    public float impactSquishMultiplier = 1.0f; // Multiplier for the jiggle when dropped

    [Header("Scoring")]
    public string itemName = "Food";
    public int baseScore = 100;

    public bool isRegisteredOnBoard { get; private set; } = false;

    private Vector3 originalScale;
    private Vector3 wobbleOffset = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (transform.position.y < -15f)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            {
                GameManager.Instance.GameOver();
            }
        }

        // Apply visual wobble only. Weight-based squishing has been removed for stability.
        transform.localScale = originalScale + wobbleOffset;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Impact wobble
        if (impactSquishMultiplier > 0.001f && collision.relativeVelocity.magnitude > 1.5f)
        {
            // Stop any existing wobble and start a new one
            StopAllCoroutines();
            StartCoroutine(WobbleRoutine(collision.relativeVelocity.magnitude));
        }
    }

    IEnumerator WobbleRoutine(float impactForce)
    {
        float squishAmount = Mathf.Clamp(impactForce * 0.03f * impactSquishMultiplier, 0.05f * impactSquishMultiplier, 0.35f * impactSquishMultiplier);
        
        float duration = 0.08f;
        float elapsed = 0f;

        // Squish down 
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            wobbleOffset.x = Mathf.Lerp(0, originalScale.x * squishAmount, t);
            wobbleOffset.y = Mathf.Lerp(0, -originalScale.y * squishAmount, t);
            yield return null;
        }

        elapsed = 0f;
        duration = 0.15f;

        // Bounce back with overshoot
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            wobbleOffset.x = Mathf.Lerp(originalScale.x * squishAmount, -originalScale.x * 0.05f * impactSquishMultiplier, t);
            wobbleOffset.y = Mathf.Lerp(-originalScale.y * squishAmount, originalScale.y * 0.05f * impactSquishMultiplier, t);
            yield return null;
        }

        elapsed = 0f;
        duration = 0.1f;

        // Settle to zero
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            wobbleOffset.x = Mathf.Lerp(-originalScale.x * 0.05f * impactSquishMultiplier, 0, t);
            wobbleOffset.y = Mathf.Lerp(originalScale.y * 0.05f * impactSquishMultiplier, 0, t);
            yield return null;
        }

        wobbleOffset = Vector3.zero;
    }

    public void OnDropped()
    {
        isRegisteredOnBoard = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterItem(this);
        }
    }

    public void OnEaten()
    {
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (isRegisteredOnBoard && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            GameManager.Instance.UnregisterItem(this);
        }
    }
}
