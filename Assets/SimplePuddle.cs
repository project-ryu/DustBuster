using UnityEngine;

public class SimplePuddle : MonoBehaviour
{
    [Header("Collection Settings")]
    public float shrinkDurationWithUpgrade = 1.5f;
    public float shrinkDurationWithoutUpgrade = 4f;

    [Header("Ripple Effect")]
    public GameObject ripplePrefab;

    private bool isInContact = false;
    private bool hasStartedCollection = false;
    private bool hasCounted = false; // For tiles counter
    private float shrinkTimer = 0f;
    private float actualShrinkDuration;
    private float lastReportedProgress = 0f; // Track progress reported to Roomba
    private Vector3 originalScale;
    private Material puddleMaterial;
    private Color originalColor;
    private RoombaController roombaController;

    void Start()
    {
        originalScale = transform.localScale;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            puddleMaterial = renderer.material;
            originalColor = puddleMaterial.color;
        }

        // Find Roomba
        GameObject roomba = GameObject.FindGameObjectWithTag("Player");
        if (roomba != null)
        {
            roombaController = roomba.GetComponent<RoombaController>();
        }
    }

    void Update()
    {
        if (hasStartedCollection && isInContact)
        {
            shrinkTimer += Time.deltaTime;
            float shrinkPercent = Mathf.Clamp01(shrinkTimer / actualShrinkDuration);

            // Log progress reporting
            if (roombaController != null && shrinkPercent > lastReportedProgress)
            {
                float progressDelta = shrinkPercent - lastReportedProgress;

                // DEBUG LOG
                if (Time.frameCount % 10 == 0) // Log every 10 frames
                {
                    Debug.Log("Puddle progress: " + shrinkPercent.ToString("F2") +
                             " | Delta: " + progressDelta.ToString("F3"));
                }

                roombaController.AddWaterProgress(progressDelta);
                lastReportedProgress = shrinkPercent;
            }

            // Finalize at 100%
            if (shrinkPercent >= 1f && !hasCounted)
            {
                Debug.Log("=== PUDDLE 100% - FINALIZING ===");
                hasCounted = true;
                FinalizeCollection();
            }

            // Visual shrinking
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, shrinkPercent);

            if (puddleMaterial != null)
            {
                Color color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0), shrinkPercent);
                puddleMaterial.color = color;
            }

            if (shrinkPercent >= 1f)
            {
                Debug.Log("Destroying puddle");
                Destroy(gameObject);
            }
        }
    }

    void FinalizeCollection()
    {
        if (roombaController != null)
        {
            roombaController.FinalizePuddle();
        }
    }

    // Remove the old CountForTiles method

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInContact = true;

            if (!hasStartedCollection)
            {
                ShopManager shop = FindFirstObjectByType<ShopManager>();
                bool hasUpgrade = (shop != null && shop.hasWaterAttachment);

                actualShrinkDuration = hasUpgrade ? shrinkDurationWithUpgrade : shrinkDurationWithoutUpgrade;

                hasStartedCollection = true;

                if (roombaController != null)
                {
                    roombaController.StartCollectingPuddle();
                }

                // TEMPORARILY DISABLE TO TEST
                // SpawnRipple();

                Debug.Log(hasUpgrade ? "Fast water collection" : "Slow water collection");
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

            GameObject ripple = Instantiate(ripplePrefab, ripplePos, Quaternion.Euler(90, 0, 0)); // Flat on ground

            // Ensure ripple doesn't spawn at charging station
            if (ripple != null)
            {
                Destroy(ripple, 2f);
            }
        }
    }
}