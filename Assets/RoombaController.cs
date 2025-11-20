using UnityEngine;
using TMPro;
using UnityEngine.UI; 

public class RoombaController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;

    [Header("Idle Mode Settings")]
    public float idleMoveSpeed = 2f;

    [Header("Battery System")]
    public float maxBattery = 100f;
    public float batteryDrainRate = 5f;
    public float batteryChargeRate = 20f;
    public float lowBatteryThreshold = 25f;

    [Header("Dust Collection")]
    public GameObject dustPuffEffect;

    [Header("Battery UI")]
    public Slider batteryGauge;
    public Image batteryFillImage; 
    private float currentBattery;
    private Rigidbody rb;
    private bool isCharging = false;
    private bool batteryDead = false;

    [Header("Water Tank")]
    public GameObject waterTank;
    public Transform waterFill;
    public TextMeshProUGUI waterTankPercentText;
    public int waterCollected = 0;
    public int maxWaterCapacity = 2;
    private float maxFillHeight = 1f;
    private float currentPuddleProgress = 0f;

    [Header("Water Physics")]
    public float normalMoveSpeed = 5f;
    public float waterSlipperiness = 0.3f;
    public float waterDrag = 0.5f;

    private bool isOnWater = false;
    private int waterPuddlesInContact = 0;

    [Header("Water Burden System")]
    private bool _hasBurden = false;
    public bool hasBurden
    {
        get { return _hasBurden; }
        set
        {
            if (_hasBurden != value)
            {
                Debug.Log("*** BURDEN CHANGED: " + _hasBurden + " → " + value + " ***");
                Debug.Log("Stack trace: " + System.Environment.StackTrace);
                _hasBurden = value;
            }
        }
    }
    public float burdenSlipperiness = 0.45f; 
    public float burdenDrag = 0.2f; 
    public int waterResistanceLevel = 0;

    [Header("Audio")]
    public AudioSource motorSound;
    public float soundFadeSpeed = 3f;
    public AudioSource idleModeMusic; // NEW
    public float musicFadeTime = 1f; // How long to fade in/out

    private float targetVolume = 0f;
    private float maxVolume = 0.5f; 
    private bool isLevelComplete = false;

    [Header("UI References")]
    public TextMeshProUGUI idleButtonText;
    public CameraController cameraController;
    public UIPositionManager uiPositionManager;
    private Vector3 targetVelocity;
    
    public bool isIdleMode = false;

    [Header("Trash Collection")]
    public AudioSource trashCollectSound; // Separate audio source for trash sound
    public AudioClip trashCollectClip; // The trash collection sound
    public GameObject trashCollectEffect; // Visual effect when collecting trash
    public int trashMoneyValue = 2; // How much money per trash pile
    public GameObject moneyPopupPrefab; // NEW: Reference to MoneyPopup prefab
    public float popupHeightOffset = 1f; // How high above trash to spawn popup

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (cameraController == null)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        if (uiPositionManager == null)
        {
            uiPositionManager = FindObjectOfType<UIPositionManager>();
        }

        if (idleModeMusic != null)
        {
            idleModeMusic.volume = 0f;
            idleModeMusic.Stop();
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ |
                        RigidbodyConstraints.FreezePositionY;

        currentBattery = maxBattery;
        normalMoveSpeed = moveSpeed;

        if (motorSound == null)
        {
            motorSound = GetComponent<AudioSource>();
        }

        if (motorSound != null)
        {
            motorSound.volume = 0f;
            motorSound.Play();
        }

        if (batteryGauge != null && batteryFillImage == null)
        {
            batteryFillImage = batteryGauge.fillRect.GetComponent<Image>();
        }

        if (waterTank != null)
        {
            waterTank.SetActive(false);
        }

        if (waterTankPercentText != null)
        {
            waterTankPercentText.gameObject.SetActive(false);
        }
        UpdateBatteryUI();

        if (waterTank != null)
        {
            Collider[] tankColliders = waterTank.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in tankColliders)
            {
                Destroy(col); // Remove colliders entirely
            }
        }
    }

    void Update()
    {
        if (isLevelComplete)
        { 
            return; 
        }
        bool isMoving = IsPlayerMoving();

        if (!isCharging && currentBattery > 0 && isMoving)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;
            currentBattery = Mathf.Max(0, currentBattery);
            UpdateBatteryUI();
        }

        if (isCharging && currentBattery < maxBattery)
        {
            currentBattery += batteryChargeRate * Time.deltaTime;
            currentBattery = Mathf.Min(maxBattery, currentBattery);
            UpdateBatteryUI();
        }

        batteryDead = currentBattery <= 0;

        UpdateMotorSound(isMoving);
    }

    bool IsPlayerMoving()
    {
        float move = Input.GetAxis("Vertical");
        float rotate = Input.GetAxis("Horizontal");

        return Mathf.Abs(move) > 0.01f || Mathf.Abs(rotate) > 0.01f;
    }

    void FixedUpdate()
    {
        if (isLevelComplete)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        if (isCharging && hasBurden)
        {
            hasBurden = false;
        }

        float move = Input.GetAxis("Vertical");
        float rotate = Input.GetAxis("Horizontal");

        rb.angularVelocity = Vector3.zero;

        float speedMultiplier = 1f;

        if (batteryDead)
        {
            speedMultiplier = 0.2f;
        }

        float currentSpeed = moveSpeed * speedMultiplier;

        float appliedSlipperiness = 0f;
        float appliedDrag = 1f;

        ShopManager shop = FindFirstObjectByType<ShopManager>();
        bool hasUpgrade = (shop != null && shop.hasWaterAttachment);

        bool shouldHaveBurden = (!hasUpgrade && isOnWater) ||
                                (hasUpgrade && waterCollected >= maxWaterCapacity);

        if (shouldHaveBurden || hasBurden)
        {
            float resistanceReduction = waterResistanceLevel * 0.15f;
            appliedSlipperiness = burdenSlipperiness * (1f - resistanceReduction);
            appliedDrag = burdenDrag * (1f + resistanceReduction * 2f);
        }
        else if (isOnWater)
        {
            appliedSlipperiness = waterSlipperiness;
            appliedDrag = waterDrag;
        }

        if (appliedSlipperiness > 0)
        {
            move *= (1f - appliedSlipperiness);
            rotate *= (1f - appliedSlipperiness);
            rb.linearVelocity *= (1f - appliedDrag * Time.fixedDeltaTime);
        }

        float turn = rotate * rotateSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);

        Vector3 moveDirection = transform.forward * move * currentSpeed;

        if (appliedSlipperiness > 0)
        {
            rb.linearVelocity += moveDirection * Time.fixedDeltaTime;

            if (rb.linearVelocity.magnitude > currentSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * currentSpeed;
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(moveDirection.x, 0, moveDirection.z);
        }

        if (isIdleMode)
        {
            // Idle mode - automatic movement
            AutomaticMovement();
        }
        else
        {
            // Manual control
            ManualMovement();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Existing dust collection code
        if (other.CompareTag("DustTile"))
        {
            if (dustPuffEffect != null)
            {
                GameObject puff = Instantiate(dustPuffEffect, other.transform.position, Quaternion.identity);
                Destroy(puff, 1f);
            }

            Destroy(other.gameObject);

            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnDustTileCleared();
            }
        }

        if (other.CompareTag("TrashPile"))
        {
            // Play satisfying collection sound
            if (trashCollectSound != null && trashCollectClip != null)
            {
                trashCollectSound.PlayOneShot(trashCollectClip);
            }
            
            // Spawn visual effect
            if (trashCollectEffect != null)
            {
                GameObject effect = Instantiate(trashCollectEffect, other.transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            
            // NEW: Spawn money popup
            if (moneyPopupPrefab != null)
            {
                // Find the Canvas to spawn the popup on
                Canvas canvas = FindFirstObjectByType<Canvas>();
                
                if (canvas != null)
                {
                    // Spawn popup as child of Canvas
                    GameObject popup = Instantiate(moneyPopupPrefab, canvas.transform);
                    
                    // Set the amount text
                    MoneyPopup popupScript = popup.GetComponent<MoneyPopup>();
                    if (popupScript != null)
                    {
                        popupScript.SetAmount(trashMoneyValue);
                        
                        // Set position based on trash world position
                        Vector3 worldPos = other.transform.position + Vector3.up * popupHeightOffset;
                        popupScript.SetWorldPosition(worldPos);
                    }
                }
            }
            
            // Give money reward
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnTrashCollected(trashMoneyValue);
            }
            
            // Destroy the trash pile
            Destroy(other.gameObject);
            
            Debug.Log("Trash collected! +$" + trashMoneyValue);
        }

        // Existing water puddle code
        if (other.CompareTag("WaterPuddle"))
        {
            waterPuddlesInContact++;

            ShopManager shop = FindFirstObjectByType<ShopManager>();
            bool hasUpgrade = (shop != null && shop.hasWaterAttachment);

            if (!hasUpgrade)
            {
                isOnWater = true;

                if (!hasBurden)
                {
                    hasBurden = true;
                    Debug.Log("NO UPGRADE - Burden applied!");
                }
            }
            else
            {
                if (waterCollected >= maxWaterCapacity)
                {
                    isOnWater = true;

                    if (!hasBurden)
                    {
                        hasBurden = true;
                        Debug.Log("TANK FULL - Burden applied!");
                    }
                }
                else
                {
                    isOnWater = false;
                    Debug.Log("Has upgrade and tank not full - NO slipperiness");
                }
            }
        }

        if (other.CompareTag("ChargingStation"))
        {
            isCharging = true;
        }
    }
    
    void ShowMoneyPopup(Vector3 position, int amount)
    {
        // This requires a TextMeshPro prefab that floats upward
        // For now, just log it
        Debug.Log("+$" + amount + "!");
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("ChargingStation"))
        {
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                Transform stationTransform = other.transform;

                Vector3 roombaPos = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 stationPos = new Vector3(stationTransform.position.x, 0, stationTransform.position.z);

                float distance = Vector3.Distance(roombaPos, stationPos);
                bool closeEnough = distance < gameManager.requiredCenterDistance;

                if (closeEnough)
                {
                    gameManager.OnReturnedToChargingStation();
                }
            }

            float drainSpeed = 0.5f;

            if (waterCollected > 0 || currentPuddleProgress > 0)
            {
                if (currentPuddleProgress > 0)
                {
                    currentPuddleProgress -= drainSpeed * Time.deltaTime;
                    if (currentPuddleProgress < 0)
                    {
                        currentPuddleProgress = 0;
                    }
                }
                else if (waterCollected > 0)
                {
                    float totalWater = waterCollected;
                    totalWater -= drainSpeed * Time.deltaTime;

                    if (totalWater <= 0)
                    {
                        waterCollected = 0;
                        currentPuddleProgress = 0;
                        Debug.Log("Water tank fully drained!");
                    }
                    else
                    {
                        waterCollected = Mathf.FloorToInt(totalWater);
                        currentPuddleProgress = totalWater - waterCollected;
                    }
                }

                UpdateWaterTankFill();
            }

            if (hasBurden)
            {
                hasBurden = false;
                Debug.Log("=== BURDEN CLEARED AT CHARGING STATION ===");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ChargingStation"))
        {
            isCharging = false;
        }

        if (other.CompareTag("WaterPuddle"))
        {
            waterPuddlesInContact--;

            if (waterPuddlesInContact <= 0)
            {
                waterPuddlesInContact = 0;
                isOnWater = false;
                Debug.Log("Left water - normal movement restored");
            }
        }
    }

    void UpdateMotorSound(bool isMoving)
    {
        if (motorSound != null)
        {
            if (isLevelComplete || motorSound == null)
            { 
                return; 
            }

            if (batteryDead)
            {
                targetVolume = 0f;
            }
            else if (isMoving)
            {
                targetVolume = maxVolume;
            }
            else
            {
                targetVolume = 0f;
            }

            motorSound.volume = Mathf.Lerp(motorSound.volume, targetVolume, soundFadeSpeed * Time.deltaTime);

            float batteryPercent = currentBattery / maxBattery;
            motorSound.pitch = Mathf.Lerp(0.7f, 1.0f, batteryPercent);

            if (!motorSound.isPlaying && !batteryDead)
            {
                motorSound.Play();
            }

            if (batteryDead && motorSound.volume < 0.01f && motorSound.isPlaying)
            {
                motorSound.Stop();
            }
        }
    }

    void UpdateBatteryUI()
    {
        if (batteryGauge != null)
        {
            batteryGauge.value = currentBattery;

            if (batteryFillImage != null)
            {
                if (batteryDead)
                {
                    batteryFillImage.color = Color.red;
                }
                else if (currentBattery <= lowBatteryThreshold)
                {
                    float t = currentBattery / lowBatteryThreshold;
                    batteryFillImage.color = Color.Lerp(Color.red, Color.yellow, t);
                }
                else
                {
                    float t = (currentBattery - lowBatteryThreshold) / (maxBattery - lowBatteryThreshold);
                    batteryFillImage.color = Color.Lerp(Color.yellow, Color.green, t);
                }
            }
        }
    }
    public void AddBattery(float amount)
    {
        currentBattery += amount;
        currentBattery = Mathf.Min(currentBattery, maxBattery); 
        UpdateBatteryUI();
    }
    void UpdateWaterTankFill()
    {
        if (waterFill == null) return;

        float totalWater = waterCollected + currentPuddleProgress;
        float fillPercent = Mathf.Clamp01(totalWater / maxWaterCapacity);

        float fillHeight = fillPercent * maxFillHeight;

        if (fillHeight <= 0.01f)
        {
            if (waterFill.gameObject.activeSelf)
            {
                waterFill.gameObject.SetActive(false);
                Debug.Log("WaterFill hidden (empty)");
            }
            return; 
        }
        else
        {
            if (!waterFill.gameObject.activeSelf)
            {
                waterFill.gameObject.SetActive(true);
                Debug.Log("WaterFill shown (has water)");
            }
        }

        waterFill.localScale = new Vector3(0.9f, fillHeight, 0.9f);
        waterFill.localPosition = new Vector3(0, -0.5f + (fillHeight / 2f), 0);

        if (waterTankPercentText != null)
        {
            int percentage = Mathf.RoundToInt(fillPercent * 100);
            waterTankPercentText.text = "Tank: " + percentage + "%";

            if (fillPercent >= 1f)
            {
                waterTankPercentText.color = Color.red;
            }
            else if (fillPercent >= 0.8f)
            {
                waterTankPercentText.color = Color.yellow;
            }
            else
            {
                waterTankPercentText.color = new Color(0, 1, 1);
            }
        }
    }

    public void ShowWaterTank()
    {
        if (waterTank != null)
        {
            waterTank.SetActive(true);
        }
    }

    public void ClearWaterBurden()
    {
        if (hasBurden)
        {
            hasBurden = false;
            Debug.Log("Water burden cleared!");
        }
    }

    public void StartCollectingPuddle()
    {
        currentPuddleProgress = 0f;
    }

    public void AddWaterProgress(float progressDelta)
    {
        float oldProgress = currentPuddleProgress;
        currentPuddleProgress += progressDelta;
        currentPuddleProgress = Mathf.Clamp01(currentPuddleProgress);

        // DEBUG
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log("AddWaterProgress: " + oldProgress.ToString("F2") + " → " +
                     currentPuddleProgress.ToString("F2") + " | Collected: " + waterCollected);
        }

        UpdateWaterTankFill();
    }

    public void FinalizePuddle()
    {
        Debug.Log("=== FINALIZE PUDDLE ===");

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnWaterPoolCleared();
        }

        currentPuddleProgress = 0f;

        waterCollected++;
        waterCollected = Mathf.Min(waterCollected, maxWaterCapacity);

        ShopManager shop = FindFirstObjectByType<ShopManager>();
        bool hasUpgrade = (shop != null && shop.hasWaterAttachment);

        if (hasUpgrade && waterCollected >= maxWaterCapacity && !hasBurden)
        {
            hasBurden = true;
            Debug.Log(">>> TANK JUST BECAME FULL - BURDEN APPLIED! <<<");
        }

        UpdateWaterTankFill();
    }

    public void SetLevelComplete()
    {
        isLevelComplete = true;
        Debug.Log("RoombaController: Level marked as complete");
    }

    public void OnLevelComplete()
    {
        isLevelComplete = true;

        if (motorSound != null)
        {
            motorSound.Stop();
            motorSound.volume = 0f;
        }
    }

    void ManualMovement()
    {
        float moveInput = Input.GetAxis("Vertical");
        float rotateInput = Input.GetAxis("Horizontal");

        // Move using Rigidbody
        Vector3 movement = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
        
        // Rotate using Rigidbody
        float rotation = rotateInput * rotateSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0, rotation, 0);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
    
    void AutomaticMovement()
    {
        // Move forward using Rigidbody
        Vector3 movement = transform.forward * idleMoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }


    void OnCollisionEnter(Collision collision)
    {
        if (!isIdleMode) return;

        // Bounce off anything that's not dust/water (which use triggers)
        // Optionally exclude the floor
        if (collision.gameObject.CompareTag("Floor"))
        {
            return; // Don't bounce off floor
        }

        // Bounce off everything else
        float bounceAngle = Random.Range(90f, 180f);
        
        if (Random.value > 0.5f)
        {
            Quaternion rotation = Quaternion.Euler(0, bounceAngle, 0);
            rb.MoveRotation(rb.rotation * rotation);
        }
        else
        {
            Quaternion rotation = Quaternion.Euler(0, -bounceAngle, 0);
            rb.MoveRotation(rb.rotation * rotation);
        }
    }

    public void ToggleIdleMode()
    {
        isIdleMode = !isIdleMode;
        
        // Update camera view
        if (cameraController != null)
        {
            cameraController.SetBirdsEyeView(isIdleMode);
        }

        if (uiPositionManager != null)
        {
            uiPositionManager.SetBirdsEyeMode(isIdleMode);
        }

        if (idleModeMusic != null)
        {
            if (isIdleMode)
            {
                // Start music with fade in
                StartCoroutine(FadeMusic(true));
            }
            else
            {
                // Stop music with fade out
                StartCoroutine(FadeMusic(false));
            }
        }
        
        if (idleButtonText != null)
        {
            idleButtonText.text = isIdleMode ? "Auto Mode: ON" : "Auto Mode: OFF";
        }
        
        Debug.Log("Idle Mode: " + (isIdleMode ? "ON" : "OFF"));
    }

    System.Collections.IEnumerator FadeMusic(bool fadeIn)
    {
        if (idleModeMusic == null) yield break;
        
        float startVolume = idleModeMusic.volume;
        float targetVolume = fadeIn ? 0.5f : 0f; // Adjust target volume as needed
        float elapsed = 0f;
        
        if (fadeIn && !idleModeMusic.isPlaying)
        {
            idleModeMusic.Play();
        }
        
        while (elapsed < musicFadeTime)
        {
            elapsed += Time.deltaTime;
            idleModeMusic.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / musicFadeTime);
            yield return null;
        }
        
        idleModeMusic.volume = targetVolume;
        
        if (!fadeIn)
        {
            idleModeMusic.Stop();
        }
    }

    public void DebugClearAllDust()
    {
        Debug.Log("=== DEBUG: CLEARING ALL DUST ===");

        GameObject[] dustTiles = GameObject.FindGameObjectsWithTag("DustTile");
        Debug.Log("Found " + dustTiles.Length + " dust tiles to clear");

        GameManager gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            foreach (GameObject tile in dustTiles)
            {
                gameManager.OnDustTileCleared(); 
                Destroy(tile);
            }

            Debug.Log("All dust cleared! Counter should now show complete.");
        }
        else
        {
            Debug.LogError("GameManager not found!");
        }
    }
}