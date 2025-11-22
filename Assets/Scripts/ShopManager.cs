using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    // ========== UPGRADE CONFIGURATION ==========
    // Edit these values to change upgrade costs and effects
    
    [Header("=== UPGRADE COSTS (Edit Here) ===")]
    [Tooltip("Cost for each upgrade level: [Level 1, Level 2, Level 3]")]
    public int[] upgradeCosts = new int[] { 100, 200, 300 };
    
    [Header("Speed Upgrade Settings")]
    public float speedIncreasePerLevel = 2f; // How much speed increases per level
    public float baseSpeed = 5f; // Starting speed
    
    [Header("Battery Upgrade Settings")]
    public float batteryIncreasePerLevel = 50f; // How much battery increases per level
    public float baseBattery = 100f; // Starting battery
    
    [Header("Vacuum Radius Upgrade Settings")]
    public float vacuumRadiusIncreasePerLevel = 0.2f; // How much radius increases per level
    public float baseVacuumRadius = 0.8f; // Starting radius
    
    [Header("Dust Capacity Upgrade Settings")]
    public int dustCapacityIncreasePerLevel = 20; // How much capacity increases per level
    public int baseDustCapacity = 10; // Starting capacity
    
    [Header("Scanner Upgrade Settings")]
    public float[] scannerRanges = new float[] { 5f, 10f, 15f }; // Range at each level
    
    // ========== UI REFERENCES ==========
    
    [Header("Shop UI")]
    public GameObject shopPanel;
    public Button shopButton;
    public Button startLevelButton;
    
    [Header("Audio")]
    public AudioClip shopOpenSound;
    public AudioClip purchaseSound;
    public AudioSource audioSource;
    
    [Header("Upgrade Buttons")]
    public Button speedUpgradeButton;
    public TextMeshProUGUI speedUpgradeText;
    
    public Button batteryUpgradeButton;
    public TextMeshProUGUI batteryUpgradeText;
    
    public Button vacuumRadiusUpgradeButton;
    public TextMeshProUGUI vacuumRadiusUpgradeText;
    
    public Button dustCapacityUpgradeButton;
    public TextMeshProUGUI dustCapacityUpgradeText;
    
    public Button scannerUpgradeButton;
    public TextMeshProUGUI scannerUpgradeText;
    
    [Header("References")]
    public RoombaController roombaController;
    public GameManager gameManager;
    public LaserScanner laserScanner;
    
    // ========== UPGRADE LEVELS (Tracked Internally) ==========
    
    private const int MAX_UPGRADE_LEVEL = 3;
    private int speedLevel = 0;
    private int batteryLevel = 0;
    private int vacuumRadiusLevel = 0;
    private int dustCapacityLevel = 0;
    private int scannerLevel = 0;
    
    private bool shopOpen = false;
    
    // ========== INITIALIZATION ==========
    
    void Start()
    {
        shopPanel.SetActive(false);
        
        LoadUpgradeData();
        
        // Connect button listeners
        if (shopButton != null)
            shopButton.onClick.AddListener(ToggleShop);
        
        if (startLevelButton != null)
            startLevelButton.onClick.AddListener(StartLevel);
        
        if (speedUpgradeButton != null)
            speedUpgradeButton.onClick.AddListener(() => BuyUpgrade("Speed"));
        
        if (batteryUpgradeButton != null)
            batteryUpgradeButton.onClick.AddListener(() => BuyUpgrade("Battery"));
        
        if (vacuumRadiusUpgradeButton != null)
            vacuumRadiusUpgradeButton.onClick.AddListener(() => BuyUpgrade("VacuumRadius"));
        
        if (dustCapacityUpgradeButton != null)
            dustCapacityUpgradeButton.onClick.AddListener(() => BuyUpgrade("DustCapacity"));
        
        if (scannerUpgradeButton != null)
            scannerUpgradeButton.onClick.AddListener(() => BuyUpgrade("Scanner"));
        
        // Validate references
        if (gameManager == null)
            Debug.LogError("ShopManager: GameManager reference is missing!");
        
        if (roombaController == null)
            Debug.LogError("ShopManager: RoombaController reference is missing!");
        
        // Apply loaded upgrades
        ApplyAllUpgrades();
        
        // Open shop at start
        shopOpen = true;
        shopPanel.SetActive(true);
        Time.timeScale = 0;
        
        // Hide gameplay UI during shop
        if (roombaController != null && roombaController.dustTankPercentText != null)
            roombaController.dustTankPercentText.gameObject.SetActive(false);
        
        if (shopButton != null)
            shopButton.gameObject.SetActive(false);
        
        UpdateUpgradeUI();
        
        // Play shop open sound
        if (audioSource != null && shopOpenSound != null)
            audioSource.PlayOneShot(shopOpenSound);
        
        Debug.Log("=== SHOP OPENED ===");
        Debug.Log("Press START LEVEL to begin!");
    }
    
    void Update()
    {
        // Toggle shop with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && !shopOpen)
        {
            ToggleShop();
        }
    }
    
    // ========== SAVE/LOAD SYSTEM ==========
    
    void LoadUpgradeData()
    {
        speedLevel = PlayerPrefs.GetInt("SpeedLevel", 0);
        batteryLevel = PlayerPrefs.GetInt("BatteryLevel", 0);
        vacuumRadiusLevel = PlayerPrefs.GetInt("VacuumRadiusLevel", 0);
        dustCapacityLevel = PlayerPrefs.GetInt("DustCapacityLevel", 0);
        scannerLevel = PlayerPrefs.GetInt("ScannerLevel", 0);
        
        Debug.Log($"Loaded upgrades - Speed:{speedLevel} Battery:{batteryLevel} Radius:{vacuumRadiusLevel} Capacity:{dustCapacityLevel} Scanner:{scannerLevel}");
    }
    
    void SaveUpgradeData()
    {
        PlayerPrefs.SetInt("SpeedLevel", speedLevel);
        PlayerPrefs.SetInt("BatteryLevel", batteryLevel);
        PlayerPrefs.SetInt("VacuumRadiusLevel", vacuumRadiusLevel);
        PlayerPrefs.SetInt("DustCapacityLevel", dustCapacityLevel);
        PlayerPrefs.SetInt("ScannerLevel", scannerLevel);
        PlayerPrefs.Save();
    }
    
    // ========== UPGRADE APPLICATION ==========
    
    void ApplyAllUpgrades()
    {
        if (roombaController == null) return;
        
        // Apply speed
        roombaController.moveSpeed = baseSpeed + (speedLevel * speedIncreasePerLevel);
        
        // Apply battery
        roombaController.maxBattery = baseBattery + (batteryLevel * batteryIncreasePerLevel);
        
        // Apply vacuum radius
        float newRadius = baseVacuumRadius + (vacuumRadiusLevel * vacuumRadiusIncreasePerLevel);
        roombaController.SetVacuumRadius(newRadius);
        
        // Apply dust capacity
        int newCapacity = baseDustCapacity + (dustCapacityLevel * dustCapacityIncreasePerLevel);
        roombaController.maxDustCapacity = newCapacity;
        
        // Apply scanner
        if (laserScanner != null && scannerLevel > 0)
            laserScanner.SetScannerLevel(scannerLevel);
        
        Debug.Log($"Applied all upgrades - Speed:{roombaController.moveSpeed} Battery:{roombaController.maxBattery} Radius:{newRadius} Capacity:{newCapacity}");
    }
    
    // ========== UNIFIED PURCHASE SYSTEM ==========
    
    void BuyUpgrade(string upgradeType)
    {
        int currentLevel = GetUpgradeLevel(upgradeType);
        
        // Check if maxed out
        if (currentLevel >= MAX_UPGRADE_LEVEL)
        {
            Debug.Log($"{upgradeType} upgrade is already maxed out!");
            return;
        }
        
        // Get cost for next level
        int cost = upgradeCosts[currentLevel]; // currentLevel is 0-indexed, so this gives us cost for next level
        
        // Check if player has enough money
        if (gameManager == null || !gameManager.SpendBotcoins(cost))
        {
            Debug.Log($"Not enough botcoins! Need ${cost}");
            return;
        }
        
        // Purchase successful!
        if (audioSource != null && purchaseSound != null)
            audioSource.PlayOneShot(purchaseSound);
        
        // Increment level
        IncrementUpgradeLevel(upgradeType);
        
        // Apply the upgrade
        ApplySpecificUpgrade(upgradeType);
        
        // Save and update UI
        SaveUpgradeData();
        UpdateUpgradeUI();
        
        Debug.Log($"{upgradeType} upgraded to level {GetUpgradeLevel(upgradeType)}!");
    }
    
    int GetUpgradeLevel(string upgradeType)
    {
        switch (upgradeType)
        {
            case "Speed": return speedLevel;
            case "Battery": return batteryLevel;
            case "VacuumRadius": return vacuumRadiusLevel;
            case "DustCapacity": return dustCapacityLevel;
            case "Scanner": return scannerLevel;
            default: return 0;
        }
    }
    
    void IncrementUpgradeLevel(string upgradeType)
    {
        switch (upgradeType)
        {
            case "Speed": speedLevel++; break;
            case "Battery": batteryLevel++; break;
            case "VacuumRadius": vacuumRadiusLevel++; break;
            case "DustCapacity": dustCapacityLevel++; break;
            case "Scanner": scannerLevel++; break;
        }
    }
    
    void ApplySpecificUpgrade(string upgradeType)
    {
        if (roombaController == null) return;
        
        switch (upgradeType)
        {
            case "Speed":
                roombaController.moveSpeed = baseSpeed + (speedLevel * speedIncreasePerLevel);
                break;
            
            case "Battery":
                float batteryIncrease = batteryIncreasePerLevel;
                roombaController.maxBattery = baseBattery + (batteryLevel * batteryIncreasePerLevel);
                roombaController.AddBattery(batteryIncrease); // Also add to current battery
                break;
            
            case "VacuumRadius":
                float newRadius = baseVacuumRadius + (vacuumRadiusLevel * vacuumRadiusIncreasePerLevel);
                roombaController.SetVacuumRadius(newRadius);
                break;
            
            case "DustCapacity":
                int newCapacity = baseDustCapacity + (dustCapacityLevel * dustCapacityIncreasePerLevel);
                roombaController.maxDustCapacity = newCapacity;
                break;
            
            case "Scanner":
                if (laserScanner != null)
                    laserScanner.SetScannerLevel(scannerLevel);
                break;
        }
    }
    
    // ========== UI MANAGEMENT ==========
    
    public void UpdateUpgradeUI()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager not connected to ShopManager!");
            return;
        }
        
        int currentCash = gameManager.botcoins;
        
        // Update Speed UI
        UpdateSingleUpgradeUI(
            speedUpgradeButton, 
            speedUpgradeText, 
            speedLevel, 
            "+SPEED", 
            $"+{speedIncreasePerLevel}/level",
            currentCash
        );
        
        // Update Battery UI
        UpdateSingleUpgradeUI(
            batteryUpgradeButton, 
            batteryUpgradeText, 
            batteryLevel, 
            "+BATTERY", 
            $"+{batteryIncreasePerLevel}/level",
            currentCash
        );
        
        // Update Vacuum Radius UI
        UpdateSingleUpgradeUI(
            vacuumRadiusUpgradeButton, 
            vacuumRadiusUpgradeText, 
            vacuumRadiusLevel, 
            "+VACUUM RADIUS", 
            $"+{vacuumRadiusIncreasePerLevel}m/level",
            currentCash
        );
        
        // Update Dust Capacity UI
        UpdateSingleUpgradeUI(
            dustCapacityUpgradeButton, 
            dustCapacityUpgradeText, 
            dustCapacityLevel, 
            "+DUST CAPACITY", 
            $"+{dustCapacityIncreasePerLevel}/level",
            currentCash
        );
        
        // Update Scanner UI
        UpdateSingleUpgradeUI(
            scannerUpgradeButton, 
            scannerUpgradeText, 
            scannerLevel, 
            "LASER SCANNER", 
            scannerLevel < MAX_UPGRADE_LEVEL ? $"{scannerRanges[scannerLevel]}m range" : "Max range",
            currentCash
        );
    }
    
    void UpdateSingleUpgradeUI(Button button, TextMeshProUGUI text, int currentLevel, string name, string description, int playerCash)
    {
        if (button == null || text == null) return;
        
        if (currentLevel >= MAX_UPGRADE_LEVEL)
        {
            // Maxed out
            text.text = $"{name}\nMAX LEVEL";
            button.interactable = false;
        }
        else
        {
            // Can still upgrade
            int cost = upgradeCosts[currentLevel];
            text.text = $"{name}\n${cost}\nLevel {currentLevel}/{MAX_UPGRADE_LEVEL}\n{description}";
            button.interactable = playerCash >= cost;
        }
    }
    
    // ========== SHOP TOGGLE ==========
    
    void ToggleShop()
    {
        shopOpen = !shopOpen;
        shopPanel.SetActive(shopOpen);
        Time.timeScale = shopOpen ? 0 : 1;
        
        // Hide/show dust tank UI
        if (roombaController != null && roombaController.dustTankPercentText != null)
            roombaController.dustTankPercentText.gameObject.SetActive(!shopOpen);
        
        if (shopOpen)
        {
            ShopButtonGlow glowEffect = shopButton != null ? shopButton.GetComponent<ShopButtonGlow>() : null;
            if (glowEffect != null)
                glowEffect.OnShopOpened();
        }
        else
        {
            Debug.Log("Shop closed - Level starting!");
            if (shopButton != null)
                shopButton.gameObject.SetActive(false);
        }
        
        UpdateUpgradeUI();
    }
    
    public void StartLevel()
    {
        Debug.Log("Starting level!");
        
        shopOpen = false;
        shopPanel.SetActive(false);
        Time.timeScale = 1;
        
        // Show gameplay UI
        if (roombaController != null && roombaController.dustTankPercentText != null)
            roombaController.dustTankPercentText.gameObject.SetActive(true);
        
        if (shopButton != null)
            shopButton.gameObject.SetActive(false);
    }
}