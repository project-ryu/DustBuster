using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("Shop UI")]
    public GameObject shopPanel;
    public Button shopButton;
    public Button startLevelButton;

    [Header("Audio")]
    public AudioClip shopOpenSound;
    public AudioClip purchaseSound;
    public AudioSource audioSource;

    [Header("Speed Upgrade")]
    public Button speedUpgradeButton;
    public TextMeshProUGUI speedUpgradeText;
    public int speedUpgradeCost = 5;
    public float speedUpgradeAmount = 1f;
    public int speedUpgradeLevel = 0;
    public int maxSpeedLevel = 5;

    [Header("Battery Upgrade")]
    public Button batteryUpgradeButton;
    public TextMeshProUGUI batteryUpgradeText;
    public int batteryUpgradeCost = 5;
    public float batteryUpgradeAmount = 20f;
    public int batteryUpgradeLevel = 0;
    public int maxBatteryLevel = 5;

    [Header("Dust Capacity Upgrade")]
    public Button dustCapacityUpgradeButton;
    public TextMeshProUGUI dustCapacityUpgradeText;
    private int dustCapacityUpgradeLevel = 0;
    private int dustCapacityUpgradeCost = 100; // Initial cost
    private const int maxDustCapacityLevel = 3;
    private const int baseDustCapacity = 1000; // Starting capacity
    private const int capacityIncreasePerLevel = 1000; // +10 dust per level

    // [Header("Water Attachment Upgrade")]
    // public Button waterAttachmentButton;
    // public TextMeshProUGUI waterAttachmentText;
    // public int waterAttachmentCost = 10; 
    // public bool hasWaterAttachment = false;

    // [Header("Water Resistance Upgrade")]
    // public Button waterResistanceButton;
    // public TextMeshProUGUI waterResistanceText;
    // public int waterResistanceCost = 8;
    // public int waterResistanceLevel = 0;
    // public int maxWaterResistanceLevel = 5;

    [Header("Scanner Upgrade")]
    public Button scannerUpgradeButton;
    public TextMeshProUGUI scannerUpgradeText;
    public LaserScanner laserScanner;
    private int scannerUpgradeLevel = 0;
    private int scannerUpgradeCost = 100; // Initial cost
    private const int maxScannerLevel = 3;

    [Header("References")]
    public RoombaController roombaController;
    public GameManager gameManager;

    private bool shopOpen = false;

    void Start()
    {
        shopPanel.SetActive(false);

        LoadUpgradeData();

        if (shopButton != null)
            shopButton.onClick.AddListener(ToggleShop);

        if (speedUpgradeButton != null)
            speedUpgradeButton.onClick.AddListener(BuySpeedUpgrade);

        if (batteryUpgradeButton != null)
            batteryUpgradeButton.onClick.AddListener(BuyBatteryUpgrade);

        // if (waterAttachmentButton != null)
        //     waterAttachmentButton.onClick.AddListener(BuyWaterAttachment);

        // if (waterResistanceButton != null)
        //     waterResistanceButton.onClick.AddListener(BuyWaterResistance);

        if (scannerUpgradeButton != null)
            scannerUpgradeButton.onClick.AddListener(BuyScannerUpgrade);

        if (dustCapacityUpgradeButton != null)
            dustCapacityUpgradeButton.onClick.AddListener(BuyDustCapacityUpgrade);

        if (startLevelButton != null)
        {
            startLevelButton.onClick.RemoveAllListeners();
            startLevelButton.onClick.AddListener(StartLevel);
            Debug.Log("StartLevel button connected");
        }
        else
        {
            Debug.LogWarning("No StartLevel button - use SPACE or ESCAPE to start");
        }

        if (gameManager == null)
        {
            Debug.LogError("ShopManager: GameManager reference is missing!");
        }

        if (roombaController == null)
        {
            Debug.LogError("ShopManager: RoombaController reference is missing!");
        }

        ApplyUpgradesToRoomba();

        shopOpen = true;
        shopPanel.SetActive(true);
        Time.timeScale = 0;

        if (shopButton != null)
        {
            shopButton.gameObject.SetActive(false);
        }

        UpdateUpgradeUI();

        if (audioSource != null && shopOpenSound != null)
        {
            audioSource.PlayOneShot(shopOpenSound);
            Debug.Log("Playing shop open sound");
        }

        Debug.Log("=== LOADOUT PHASE ===");
        Debug.Log("Press SPACE, ESCAPE, or click START LEVEL to begin!");
    }

    void LoadUpgradeData()
    {
        speedUpgradeLevel = PlayerPrefs.GetInt("SpeedLevel", 0);
        speedUpgradeCost = PlayerPrefs.GetInt("SpeedCost", 100);

        batteryUpgradeLevel = PlayerPrefs.GetInt("BatteryLevel", 0);
        batteryUpgradeCost = PlayerPrefs.GetInt("BatteryCost", 100);

        // hasWaterAttachment = PlayerPrefs.GetInt("HasWaterAttachment", 0) == 1;

        // waterResistanceLevel = PlayerPrefs.GetInt("WaterResistanceLevel", 0);
        // waterResistanceCost = PlayerPrefs.GetInt("WaterResistanceCost", 8);

        scannerUpgradeLevel = PlayerPrefs.GetInt("ScannerLevel", 0);
        scannerUpgradeCost = PlayerPrefs.GetInt("ScannerCost", 100);

        dustCapacityUpgradeLevel = PlayerPrefs.GetInt("DustCapacityLevel", 0);
        dustCapacityUpgradeCost = PlayerPrefs.GetInt("DustCapacityCost", 100);
        int currentCapacity = baseDustCapacity + (dustCapacityUpgradeLevel * capacityIncreasePerLevel);
        if (roombaController != null)
        {
            roombaController.maxDustCapacity = currentCapacity;
        }

        Debug.Log("Loaded dust capacity level: " + dustCapacityUpgradeLevel + " (Capacity: " + currentCapacity + ")");

        Debug.Log("Loaded upgrades - Speed: " + speedUpgradeLevel +
                  ", Battery: " + batteryUpgradeLevel);
    }

    void SaveUpgradeData()
    {
        PlayerPrefs.SetInt("SpeedLevel", speedUpgradeLevel);
        PlayerPrefs.SetInt("SpeedCost", speedUpgradeCost);

        PlayerPrefs.SetInt("BatteryLevel", batteryUpgradeLevel);
        PlayerPrefs.SetInt("BatteryCost", batteryUpgradeCost);

        // PlayerPrefs.SetInt("HasWaterAttachment", hasWaterAttachment ? 1 : 0);

        // PlayerPrefs.SetInt("WaterResistanceLevel", waterResistanceLevel);
        // PlayerPrefs.SetInt("WaterResistanceCost", waterResistanceCost);

        PlayerPrefs.SetInt("ScannerLevel", scannerUpgradeLevel);

        PlayerPrefs.SetInt("DustCapacityLevel", dustCapacityUpgradeLevel);

        PlayerPrefs.Save();
    }

    void ApplyUpgradesToRoomba()
    {
        if (roombaController == null) return;
        float baseSpeed = 5f;
        roombaController.moveSpeed = baseSpeed + (speedUpgradeLevel * speedUpgradeAmount);

        float baseBattery = 100f;
        roombaController.maxBattery = baseBattery + (batteryUpgradeLevel * batteryUpgradeAmount);

        // if (hasWaterAttachment)
        // {
        //     roombaController.ShowWaterTank();
        // }

        // roombaController.waterResistanceLevel = waterResistanceLevel;

        Debug.Log("Applied upgrades to Roomba - Speed: " + roombaController.moveSpeed +
                  ", Battery: " + roombaController.maxBattery);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && shopOpen)
        {
            ToggleShop();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && shopOpen)
        {
            ToggleShop();
        }
    }

    void ToggleShop()
    {
        shopOpen = !shopOpen;
        shopPanel.SetActive(shopOpen);

        Time.timeScale = shopOpen ? 0 : 1;

        if (shopOpen)
        {
            ShopButtonGlow glowEffect = shopButton != null ? shopButton.GetComponent<ShopButtonGlow>() : null;
            if (glowEffect != null)
            {
                glowEffect.OnShopOpened();
            }
        }
        else
        {
            Debug.Log("Shop closed - Level starting!");

            if (shopButton != null)
            {
                shopButton.gameObject.SetActive(false);
            }
        }

        UpdateUpgradeUI();
    }

    public void StartLevel()
    {
        Debug.Log("Starting level!");

        shopOpen = false;
        shopPanel.SetActive(false);
        Time.timeScale = 1; 

        if (shopButton != null)
        {
            shopButton.gameObject.SetActive(false);
        }
    }

    void BuySpeedUpgrade()
    {
        if (speedUpgradeLevel >= maxSpeedLevel)
        {
            Debug.Log("Speed upgrade maxed out!");
            return;
        }

        if (gameManager.SpendBotcoins(speedUpgradeCost))
        {
            if (audioSource != null && purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            roombaController.moveSpeed += speedUpgradeAmount;
            speedUpgradeLevel++;
            speedUpgradeCost += 5;

            SaveUpgradeData(); 
            UpdateUpgradeUI();

            Debug.Log("Speed upgraded! New speed: " + roombaController.moveSpeed);
        }
        else
        {
            Debug.Log("Not enough money! Need $" + speedUpgradeCost);
        }
    }

    void BuyBatteryUpgrade()
    {
        if (batteryUpgradeLevel >= maxBatteryLevel)
        {
            Debug.Log("Battery upgrade maxed out!");
            return;
        }

        if (gameManager.SpendBotcoins(batteryUpgradeCost))
        {
            if (audioSource != null && purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            roombaController.maxBattery += batteryUpgradeAmount;
            roombaController.AddBattery(batteryUpgradeAmount);

            batteryUpgradeLevel++;
            batteryUpgradeCost += 5;

            SaveUpgradeData(); 
            UpdateUpgradeUI();

            Debug.Log("Battery upgraded! New max battery: " + roombaController.maxBattery);
        }
        else
        {
            Debug.Log("Not enough money! Need $" + batteryUpgradeCost);
        }
    }

    // void BuyWaterAttachment()
    // {
    //     if (hasWaterAttachment)
    //     {
    //         Debug.Log("Already have water attachment!");
    //         return;
    //     }

    //     if (gameManager.SpendBotcoins(waterAttachmentCost))
    //     {
    //         if (audioSource != null && purchaseSound != null)
    //         {
    //             audioSource.PlayOneShot(purchaseSound);
    //         }
    //         hasWaterAttachment = true;

    //         if (roombaController != null)
    //         {
    //             roombaController.ShowWaterTank();
    //             roombaController.ClearWaterBurden();
    //             Debug.Log("Water attachment purchased - burden cleared!");
    //         }

    //         SaveUpgradeData(); 
    //         UpdateUpgradeUI();
    //     }
    //     else
    //     {
    //         Debug.Log("Not enough money! Need $" + waterAttachmentCost);
    //     }
    // }

    // void BuyWaterResistance()
    // {
    //     if (waterResistanceLevel >= maxWaterResistanceLevel)
    //     {
    //         Debug.Log("Water resistance maxed out!");
    //         return;
    //     }

    //     if (gameManager.SpendBotcoins(waterResistanceCost))
    //     {
    //         if (audioSource != null && purchaseSound != null)
    //         {
    //             audioSource.PlayOneShot(purchaseSound);
    //         }
    //         waterResistanceLevel++;

    //         if (roombaController != null)
    //         {
    //             roombaController.waterResistanceLevel = waterResistanceLevel;
    //         }

    //         waterResistanceCost += 5;

    //         SaveUpgradeData();
    //         UpdateUpgradeUI();

    //         Debug.Log("Water resistance upgraded! Level: " + waterResistanceLevel);
    //     }
    //     else
    //     {
    //         Debug.Log("Not enough money! Need $" + waterResistanceCost);
    //     }
    // }

    void BuyScannerUpgrade()
    {
        if (scannerUpgradeLevel >= maxScannerLevel)
        {
            Debug.Log("Scanner upgrade maxed out!");
            return;
        }
        
        if (gameManager.SpendBotcoins(scannerUpgradeCost))
        {
            // Play purchase sound if you have it
            if (audioSource != null && purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            
            scannerUpgradeLevel++;
            laserScanner.SetScannerLevel(scannerUpgradeLevel);
            
            // Increase cost for next level
            scannerUpgradeCost += 100;
            
            SaveUpgradeData();
            UpdateUpgradeUI();
            
            Debug.Log($"Scanner upgraded to level {scannerUpgradeLevel}!");
        }
        else
        {
            Debug.Log("Not enough botcoins! Need $" + scannerUpgradeCost);
        }
    }

    void BuyDustCapacityUpgrade()
    {
        if (dustCapacityUpgradeLevel >= maxDustCapacityLevel)
        {
            Debug.Log("Dust capacity upgrade maxed out!");
            return;
        }
        
        if (gameManager.SpendBotcoins(dustCapacityUpgradeCost))
        {
            // Play purchase sound
            if (audioSource != null && purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            
            dustCapacityUpgradeLevel++;
            
            // Calculate new capacity
            int newCapacity = baseDustCapacity + (dustCapacityUpgradeLevel * capacityIncreasePerLevel);
            
            // Apply to Roomba
            if (roombaController != null)
            {
                roombaController.UpgradeDustCapacity(newCapacity);
            }
            
            // Increase cost for next level
            dustCapacityUpgradeCost += 5;
            
            SaveUpgradeData();
            UpdateUpgradeUI();
            
            Debug.Log($"Dust capacity upgraded to level {dustCapacityUpgradeLevel}! New capacity: {newCapacity}");
        }
        else
        {
            Debug.Log("Not enough botcoins! Need $" + dustCapacityUpgradeCost);
        }
    }

    public void UpdateUpgradeUI()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager not connected to ShopManager!");
            return;
        }

        int currentCash = gameManager.botcoins;

        if (speedUpgradeButton != null)
        {
            if (speedUpgradeLevel >= maxSpeedLevel)
            {
                speedUpgradeText.text = "+SPEED\nMAX LEVEL";
                speedUpgradeButton.interactable = false;
            }
            else
            {
                speedUpgradeText.text = "+SPEED\n$" + speedUpgradeCost +
                                       "\nLevel: " + speedUpgradeLevel + "/" + maxSpeedLevel;
                speedUpgradeButton.interactable = currentCash >= speedUpgradeCost;
            }
        }
        if (batteryUpgradeButton != null)
        {
            if (batteryUpgradeLevel >= maxBatteryLevel)
            {
                batteryUpgradeText.text = "+BATTERY\nMAX LEVEL";
                batteryUpgradeButton.interactable = false;
            }
            else
            {
                batteryUpgradeText.text = "+BATTERY\n$" + batteryUpgradeCost +
                                         "\nLevel: " + batteryUpgradeLevel + "/" + maxBatteryLevel;
                batteryUpgradeButton.interactable = currentCash >= batteryUpgradeCost;
            }
        }

        // if (waterAttachmentButton != null)
        // {
        //     if (hasWaterAttachment)
        //     {
        //         waterAttachmentText.text = "+WATER VAC\nPURCHASED";
        //         waterAttachmentButton.interactable = false;
        //     }
        //     else
        //     {
        //         waterAttachmentText.text = "+WATER VAC\n$" + waterAttachmentCost;
        //         waterAttachmentButton.interactable = currentCash >= waterAttachmentCost;
        //     }
        // }

        // if (waterResistanceButton != null)
        // {
        //     if (waterResistanceLevel >= maxWaterResistanceLevel)
        //     {
        //         waterResistanceText.text = "+WATER RESIST\nMAX LEVEL";
        //         waterResistanceButton.interactable = false;
        //     }
        //     else
        //     {
        //         waterResistanceText.text = "+WATER RESIST\n$" + waterResistanceCost +
        //                                   "\nLevel: " + waterResistanceLevel + "/" + maxWaterResistanceLevel;
        //         waterResistanceButton.interactable = currentCash >= waterResistanceCost;
        //     }
        // }

        if (scannerUpgradeButton != null)
        {
            if (scannerUpgradeLevel >= maxScannerLevel)
            {
                scannerUpgradeText.text = "+SCANNER\nMAX LEVEL";
                scannerUpgradeButton.interactable = false;
            }
            else
            {
                scannerUpgradeText.text = "+SCANNER\n$" + scannerUpgradeCost +
                                        "\nLevel: " + scannerUpgradeLevel + "/" + maxScannerLevel;
                scannerUpgradeButton.interactable = currentCash >= scannerUpgradeCost;
            }
        }

        if (dustCapacityUpgradeButton != null)
        {
            if (dustCapacityUpgradeLevel >= maxDustCapacityLevel)
            {
                dustCapacityUpgradeText.text = "+CAPACITY\nMAX LEVEL";
                dustCapacityUpgradeButton.interactable = false;
            }
            else
            {
                dustCapacityUpgradeText.text = "+CAPACITY\n$" + dustCapacityUpgradeCost +
                                        "\nLevel: " + dustCapacityUpgradeLevel + "/" + maxDustCapacityLevel;
                dustCapacityUpgradeButton.interactable = currentCash >= dustCapacityUpgradeCost;
            }
        }
    }
}