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

    [Header("Dust Tank System")]
    public GameObject dustTank; // The parent GameObject
    public GameObject tankContainer; // NEW: The visual container cube
    public Transform dustFill; // The fill indicator inside the tank
    public TextMeshProUGUI dustTankPercentText;
    public int dustCollected = 0;
    public int maxDustCapacity = 10;
    private float maxDustFillHeight = 1f;
    private bool isTankFull = false;

    [Header("Vacuum Radius")]
    public CapsuleCollider vacuumCollider; // The trigger collider that picks up dust
    private float baseVacuumRadius = 0.8f; // Starting radius

    [Header("Turbo Boost")]
    public KeyCode turboKey = KeyCode.LeftShift;
    public float turboSpeedMultiplier = 2.5f; // 2.5x normal speed
    public float turboBatteryDrainMultiplier = 5f; // Drains 5x faster
    public bool isTurboActive = false;

    [Header("Turbo Audio")]
    public AudioSource turboSound; // Optional: engine revving sound
    public float turboSoundPitch = 1.5f;

    [Header("Turbo Visual")]
    public GameObject turboModule;
    public Material turboActiveMaterial;
    public Material turboInactiveMaterial;
    public bool pulseTurboGlow = true;
    public float pulseSpeed = 3f;
    private Renderer turboRenderer;
    private Material turboMaterialInstance; // Instance to modify

    // [Header("Water Tank")]
    // public GameObject waterTank;
    // public Transform waterFill;
    // public TextMeshProUGUI waterTankPercentText;
    // public int waterCollected = 0;
    // public int maxWaterCapacity = 2;
    // private float maxFillHeight = 1f;
    // private float currentPuddleProgress = 0f;

    // [Header("Water Physics")]
    // public float normalMoveSpeed = 5f;
    // public float waterSlipperiness = 0.3f;
    // public float waterDrag = 0.5f;

    // private bool isOnWater = false;
    // private int waterPuddlesInContact = 0;

    // [Header("Water Burden System")]
    // private bool _hasBurden = false;
    // public bool hasBurden
    // {
    //     get { return _hasBurden; }
    //     set
    //     {
    //         if (_hasBurden != value)
    //         {
    //             Debug.Log("*** BURDEN CHANGED: " + _hasBurden + " → " + value + " ***");
    //             Debug.Log("Stack trace: " + System.Environment.StackTrace);
    //             _hasBurden = value;
    //         }
    //     }
    // }
    // public float burdenSlipperiness = 0.45f; 
    // public float burdenDrag = 0.2f; 
    // public int waterResistanceLevel = 0;

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
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Reset center of mass to prevent jitter from child objects
            rb.ResetCenterOfMass();
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

        if (turboModule != null)
        {
            turboRenderer = turboModule.GetComponent<Renderer>();
            
            // Start with inactive material
            if (turboRenderer != null && turboInactiveMaterial != null)
            {
                turboRenderer.material = turboInactiveMaterial;
            }
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ |
                        RigidbodyConstraints.FreezePositionY;

        currentBattery = maxBattery;
        // normalMoveSpeed = moveSpeed;

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

        if (dustTank != null)
        {
            dustTank.SetActive(true); // Always visible
        }

        if (dustTankPercentText != null)
        {
            dustTankPercentText.gameObject.SetActive(true);
        }

        UpdateDustTankVisuals();

        // Remove any colliders from dust tank so it doesn't interfere
        if (dustTank != null)
        {
            Collider[] tankColliders = dustTank.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in tankColliders)
            {
                Destroy(col);
            }
        }

        // if (waterTank != null)
        // {
        //     waterTank.SetActive(false);
        // }

        // if (waterTankPercentText != null)
        // {
        //     waterTankPercentText.gameObject.SetActive(false);
        // }

        // if (waterTank != null)
        // {
        //     Collider[] tankColliders = waterTank.GetComponentsInChildren<Collider>(true);
        //     foreach (Collider col in tankColliders)
        //     {
        //         Destroy(col); // Remove colliders entirely
        //     }
        // }
        UpdateBatteryUI();
    }

    void Update()
    {
        if (isLevelComplete)
        { 
            return; 
        }
        
        // Check for turbo boost input
        bool wantsTurbo = Input.GetKey(turboKey) && !batteryDead && !isIdleMode;
        
        // Update turbo state
        if (wantsTurbo && currentBattery > 0)
        {
            if (!isTurboActive)
            {
                isTurboActive = true;
                OnTurboActivated();
            }
        }
        else
        {
            if (isTurboActive)
            {
                isTurboActive = false;
                OnTurboDeactivated();
            }
        }

        // Pulse the turbo glow when active
        if (isTurboActive && pulseTurboGlow && turboRenderer != null)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f) * 0.5f + 0.5f; // 0.5 to 1.0
            Color emissionColor = Color.red * pulse * 3f; // Pulse between dim and bright
            
            if (turboMaterialInstance != null)
            {
                turboMaterialInstance.SetColor("_EmissionColor", emissionColor);
            }
        }
        
        bool isMoving = IsPlayerMoving();

        // Battery drain - increased when using turbo
        if (!isCharging && currentBattery > 0 && isMoving)
        {
            float drainRate = isTurboActive ? batteryDrainRate * turboBatteryDrainMultiplier : batteryDrainRate;
            currentBattery -= drainRate * Time.deltaTime;
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

        // Debug
        if (Input.GetKeyDown(KeyCode.P) && tankContainer != null)
        {
            Debug.Log("=== TANK DEBUG ===");
            Debug.Log("Current scale: " + tankContainer.transform.localScale);
            Debug.Log("Max capacity: " + maxDustCapacity);
            Debug.Log("Current dust: " + dustCollected);
        }
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

        // ALWAYS clear angular velocity first
        rb.angularVelocity = Vector3.zero;

        float move = Input.GetAxis("Vertical");
        float rotate = Input.GetAxis("Horizontal");

        float speedMultiplier = batteryDead ? 0.2f : 1f;
        
        if (isTurboActive)
        {
            speedMultiplier *= turboSpeedMultiplier;
        }
        
        float currentSpeed = moveSpeed * speedMultiplier;

        // Rotation
        float turn = rotate * rotateSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
        
        // Clear angular velocity again
        rb.angularVelocity = Vector3.zero;

        // Movement - INSTANT response (no acceleration)
        Vector3 targetVelocity;
        
        if (isIdleMode)
        {
            targetVelocity = transform.forward * idleMoveSpeed;
        }
        else
        {
            targetVelocity = transform.forward * move * currentSpeed;
        }
        
        // Direct velocity assignment for instant response
        rb.linearVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DustTile"))
        {
            // Can't vacuum during turbo boost!
            if (isTurboActive)
            {
                Debug.Log("Can't vacuum during turbo boost!");
                return;
            }
            
            // Check if tank is full
            if (isTankFull)
            {
                Debug.Log("Dust tank is FULL! Can't collect more dust. Empty at charging station!");
                return;
            }

            // Spawn dust puff effect
            if (dustPuffEffect != null)
            {
                GameObject puff = Instantiate(dustPuffEffect, other.transform.position, Quaternion.identity);
                Destroy(puff, 1f);
            }

            // Destroy the dust tile
            Destroy(other.gameObject);

            // Add to dust tank
            dustCollected++;
            UpdateDustTankVisuals();

            // Check if tank is now full
            if (dustCollected >= maxDustCapacity)
            {
                isTankFull = true;
                Debug.Log("*** DUST TANK NOW FULL! ***");
            }

            // Notify GameManager
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

        // if (other.CompareTag("WaterPuddle"))
        // {
        //     waterPuddlesInContact++;

        //     ShopManager shop = FindFirstObjectByType<ShopManager>();
        //     bool hasUpgrade = (shop != null && shop.hasWaterAttachment);

        //     if (!hasUpgrade)
        //     {
        //         isOnWater = true;

        //         if (!hasBurden)
        //         {
        //             hasBurden = true;
        //             Debug.Log("NO UPGRADE - Burden applied!");
        //         }
        //     }
        //     else
        //     {
        //         if (waterCollected >= maxWaterCapacity)
        //         {
        //             isOnWater = true;

        //             if (!hasBurden)
        //             {
        //                 hasBurden = true;
        //                 Debug.Log("TANK FULL - Burden applied!");
        //             }
        //         }
        //         else
        //         {
        //             isOnWater = false;
        //             Debug.Log("Has upgrade and tank not full - NO slipperiness");
        //         }
        //     }
        // }

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
            
            if (dustCollected > 0)
            {
                float emptySpeed = 5f; // How fast to empty (dust per second)
                float dustToEmpty = emptySpeed * Time.deltaTime;
                
                dustCollected -= Mathf.CeilToInt(dustToEmpty);
                dustCollected = Mathf.Max(0, dustCollected);
                
                UpdateDustTankVisuals();
                
                if (dustCollected <= 0)
                {
                    dustCollected = 0;
                    isTankFull = false;
                    Debug.Log("Dust tank emptied!");
                }
            }

            // float drainSpeed = 0.5f;

            // if (waterCollected > 0 || currentPuddleProgress > 0)
            // {
            //     if (currentPuddleProgress > 0)
            //     {
            //         currentPuddleProgress -= drainSpeed * Time.deltaTime;
            //         if (currentPuddleProgress < 0)
            //         {
            //             currentPuddleProgress = 0;
            //         }
            //     }
            //     else if (waterCollected > 0)
            //     {
            //         float totalWater = waterCollected;
            //         totalWater -= drainSpeed * Time.deltaTime;

            //         if (totalWater <= 0)
            //         {
            //             waterCollected = 0;
            //             currentPuddleProgress = 0;
            //             Debug.Log("Water tank fully drained!");
            //         }
            //         else
            //         {
            //             waterCollected = Mathf.FloorToInt(totalWater);
            //             currentPuddleProgress = totalWater - waterCollected;
            //         }
            //     }

            //     UpdateWaterTankFill();
            // }

            // if (hasBurden)
            // {
            //     hasBurden = false;
            //     Debug.Log("=== BURDEN CLEARED AT CHARGING STATION ===");
            // }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ChargingStation"))
        {
            isCharging = false;
        }

        // if (other.CompareTag("WaterPuddle"))
        // {
        //     waterPuddlesInContact--;

        //     if (waterPuddlesInContact <= 0)
        //     {
        //         waterPuddlesInContact = 0;
        //         isOnWater = false;
        //         Debug.Log("Left water - normal movement restored");
        //     }
        // }
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

    // void UpdateWaterTankFill()
    // {
    //     if (waterFill == null) return;

    //     float totalWater = waterCollected + currentPuddleProgress;
    //     float fillPercent = Mathf.Clamp01(totalWater / maxWaterCapacity);

    //     float fillHeight = fillPercent * maxFillHeight;

    //     if (fillHeight <= 0.01f)
    //     {
    //         if (waterFill.gameObject.activeSelf)
    //         {
    //             waterFill.gameObject.SetActive(false);
    //             Debug.Log("WaterFill hidden (empty)");
    //         }
    //         return; 
    //     }
    //     else
    //     {
    //         if (!waterFill.gameObject.activeSelf)
    //         {
    //             waterFill.gameObject.SetActive(true);
    //             Debug.Log("WaterFill shown (has water)");
    //         }
    //     }

    //     waterFill.localScale = new Vector3(0.9f, fillHeight, 0.9f);
    //     waterFill.localPosition = new Vector3(0, -0.5f + (fillHeight / 2f), 0);

    //     if (waterTankPercentText != null)
    //     {
    //         int percentage = Mathf.RoundToInt(fillPercent * 100);
    //         waterTankPercentText.text = "Tank: " + percentage + "%";

    //         if (fillPercent >= 1f)
    //         {
    //             waterTankPercentText.color = Color.red;
    //         }
    //         else if (fillPercent >= 0.8f)
    //         {
    //             waterTankPercentText.color = Color.yellow;
    //         }
    //         else
    //         {
    //             waterTankPercentText.color = new Color(0, 1, 1);
    //         }
    //     }
    // }

    // public void ShowWaterTank()
    // {
    //     if (waterTank != null)
    //     {
    //         waterTank.SetActive(true);
    //     }
    // }

    // public void ClearWaterBurden()
    // {
    //     if (hasBurden)
    //     {
    //         hasBurden = false;
    //         Debug.Log("Water burden cleared!");
    //     }
    // }

    // public void StartCollectingPuddle()
    // {
    //     currentPuddleProgress = 0f;
    // }

    // public void AddWaterProgress(float progressDelta)
    // {
    //     float oldProgress = currentPuddleProgress;
    //     currentPuddleProgress += progressDelta;
    //     currentPuddleProgress = Mathf.Clamp01(currentPuddleProgress);

    //     // DEBUG
    //     if (Time.frameCount % 30 == 0)
    //     {
    //         Debug.Log("AddWaterProgress: " + oldProgress.ToString("F2") + " → " +
    //                  currentPuddleProgress.ToString("F2") + " | Collected: " + waterCollected);
    //     }
    // }

    // public void FinalizePuddle()
    // {
    //     Debug.Log("=== FINALIZE PUDDLE ===");

    //     GameManager gameManager = FindFirstObjectByType<GameManager>();
    //     if (gameManager != null)
    //     {
    //         gameManager.OnWaterPoolCleared();
    //     }

    //     currentPuddleProgress = 0f;

    //     waterCollected++;
    //     waterCollected = Mathf.Min(waterCollected, maxWaterCapacity);

    //     ShopManager shop = FindFirstObjectByType<ShopManager>();
    //     bool hasUpgrade = (shop != null && shop.hasWaterAttachment);

    //     if (hasUpgrade && waterCollected >= maxWaterCapacity && !hasBurden)
    //     {
    //         hasBurden = true;
    //         Debug.Log(">>> TANK JUST BECAME FULL - BURDEN APPLIED! <<<");
    //     }

    //     UpdateWaterTankFill();
    // }

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

    void OnCollisionEnter(Collision collision)
    {
        // ALWAYS clear angular velocity on any collision
        rb.angularVelocity = Vector3.zero;
        
        // Don't interfere with idle mode bouncing
        if (isIdleMode)
        {
            if (collision.gameObject.CompareTag("Floor"))
            {
                return;
            }

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
            
            // Clear velocity after bounce
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero; // Clear again after bounce
            return;
        }

        // For normal mode: dampen velocity on wall hits
        if (!collision.gameObject.CompareTag("Floor"))
        {
            Vector3 normal = collision.contacts[0].normal;
            Vector3 velocity = rb.linearVelocity;
            velocity = Vector3.ProjectOnPlane(velocity, normal) * 0.3f;
            rb.linearVelocity = velocity;
            
            // Make sure rotation stops too
            rb.angularVelocity = Vector3.zero;
            
            Debug.Log("Hit wall - dampening velocity and clearing rotation");
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Always clear angular velocity when touching walls
        rb.angularVelocity = Vector3.zero;
        
        // If we're still touching a wall and not moving forward, clear velocity
        if (!collision.gameObject.CompareTag("Floor") && !isIdleMode)
        {
            float moveInput = Input.GetAxis("Vertical");
            
            // If player isn't trying to move forward, clear lateral velocity
            if (Mathf.Abs(moveInput) < 0.1f)
            {
                rb.linearVelocity = Vector3.zero;
            }
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
   
    void UpdateDustTankVisuals()
    {
        if (dustFill == null) return;

        float fillPercent = Mathf.Clamp01((float)dustCollected / maxDustCapacity);

        if (fillPercent <= 0.01f)
        {
            // Empty - hide fill
            if (dustFill.gameObject.activeSelf)
            {
                dustFill.gameObject.SetActive(false);
            }
        }
        else
        {
            // Has dust - show fill
            if (!dustFill.gameObject.activeSelf)
            {
                dustFill.gameObject.SetActive(true);
            }
            
            // Get the actual height of the tank container
            float tankHeight = 1f; // Default assumption
            if (tankContainer != null)
            {
                tankHeight = tankContainer.transform.localScale.y;
            }
            
            // Calculate fill height based on actual tank height
            float fillHeight = fillPercent * tankHeight;
            
            // Scale the fill - width matches tank, height grows
            float tankWidth = tankContainer != null ? tankContainer.transform.localScale.x : 1f;
            float tankDepth = tankContainer != null ? tankContainer.transform.localScale.z : 1f;
            dustFill.localScale = new Vector3(tankWidth * 0.9f, fillHeight, tankDepth * 0.9f);
            
            // Position: Start at bottom of tank and grow upward
            float yPosition = -(tankHeight / 2f) + (fillHeight / 2f);
            dustFill.localPosition = new Vector3(0, yPosition, 0);
        }

        // Update text UI
        if (dustTankPercentText != null)
        {
            int percentage = Mathf.RoundToInt(fillPercent * 100);
            dustTankPercentText.text = percentage + "%";

            if (fillPercent >= 1f)
                dustTankPercentText.color = Color.red;
            else if (fillPercent >= 0.8f)
                dustTankPercentText.color = Color.yellow;
            else
                dustTankPercentText.color = Color.white;
        }
    }

    public void UpgradeDustCapacity(int newCapacity)
    {
        maxDustCapacity = newCapacity;
        
        // DON'T scale the visual - just increase capacity
        // The tank stays the same size, but holds more dust
        
        UpdateDustTankVisuals();
        Debug.Log("Dust tank capacity upgraded to: " + newCapacity);
    }

    public void EmptyDustTank()
    {
        dustCollected = 0;
        isTankFull = false;
        UpdateDustTankVisuals();
        Debug.Log("Dust tank manually emptied");
    }

    public void SetVacuumRadius(float newRadius)
    {
        if (vacuumCollider != null)
        {
            vacuumCollider.radius = newRadius;
            Debug.Log($"Vacuum radius set to: {newRadius}");
        }
        else
        {
            Debug.LogWarning("Vacuum collider not assigned! Cannot change radius.");
        }
    }

    void OnTurboActivated()
    {
        Debug.Log("🚀 TURBO BOOST ACTIVATED!");
        
        if (turboRenderer != null && turboActiveMaterial != null)
        {
            // Create instance to modify
            turboMaterialInstance = new Material(turboActiveMaterial);
            turboRenderer.material = turboMaterialInstance;
        }
        
        if (motorSound != null)
        {
            motorSound.pitch = turboSoundPitch;
        }
        
        if (turboSound != null && !turboSound.isPlaying)
        {
            turboSound.Play();
        }
    }

    void OnTurboDeactivated()
    {
        Debug.Log("Turbo boost deactivated");
        
        if (turboRenderer != null && turboInactiveMaterial != null)
        {
            turboRenderer.material = turboInactiveMaterial;
            
            // Clean up instance
            if (turboMaterialInstance != null)
            {
                Destroy(turboMaterialInstance);
                turboMaterialInstance = null;
            }
        }
        
        if (motorSound != null)
        {
            float batteryPercent = currentBattery / maxBattery;
            motorSound.pitch = Mathf.Lerp(0.7f, 1.0f, batteryPercent);
        }
        
        if (turboSound != null && turboSound.isPlaying)
        {
            turboSound.Stop();
        }
    }
}