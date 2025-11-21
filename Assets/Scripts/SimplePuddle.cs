using UnityEngine;

public class SimplePuddle : MonoBehaviour
{
    [Header("Collection Settings")]
    public float shrinkDuration = 1.5f; // How long it takes to collect
    public int puddleValue = 1; // Botcoins earned for collecting this puddle

    [Header("Ripple Effect")]
    public GameObject ripplePrefab;

    private bool isInContact = false;
    private bool hasStartedCollection = false;
    private bool hasBeenCollected = false;
    private float shrinkTimer = 0f;
    private Vector3 originalScale;
    private Material puddleMaterial;
    private Color originalColor;

    void Start()
    {
        originalScale = transform.localScale;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            puddleMaterial = renderer.material;
            originalColor = puddleMaterial.color;
        }
    }

    void Update()
    {
        if (hasStartedCollection && isInContact && !hasBeenCollected)
        {
            shrinkTimer += Time.deltaTime;
            float shrinkPercent = Mathf.Clamp01(shrinkTimer / shrinkDuration);

            // Visual shrinking
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, shrinkPercent);

            // Fade out color
            if (puddleMaterial != null)
            {
                Color color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0), shrinkPercent);
                puddleMaterial.color = color;
            }

            // When fully collected
            if (shrinkPercent >= 1f && !hasBeenCollected)
            {
                hasBeenCollected = true;
                CollectPuddle();
            }
        }
    }

    void CollectPuddle()
    {
        Debug.Log("Water puddle collected!");

        // Notify GameManager that puddle was collected
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnWaterPoolCleared(); // Count toward level completion
            
            // Optional: Give botcoins reward
            if (puddleValue > 0)
            {
                gameManager.AddBotcoins(puddleValue);
                Debug.Log("Earned $" + puddleValue + " from water puddle!");
            }
        }

        // Destroy the puddle
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInContact = true;

            if (!hasStartedCollection)
            {
                hasStartedCollection = true;
                SpawnRipple();
                Debug.Log("Started collecting water puddle");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInContact = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInContact = false;
            Debug.Log("Left water puddle - collection paused");
        }
    }

    void SpawnRipple()
    {
        if (ripplePrefab != null)
        {
            Vector3 ripplePos = transform.position;
            ripplePos.y = 0.05f; // Just above ground

            GameObject ripple = Instantiate(ripplePrefab, ripplePos, Quaternion.Euler(90, 0, 0));

            if (ripple != null)
            {
                Destroy(ripple, 2f);
            }
        }
    }
}