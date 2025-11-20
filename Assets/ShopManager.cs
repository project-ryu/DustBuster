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

    [Header("Water Attachment Upgrade")]
    public Button waterAttachmentButton;
    public TextMeshProUGUI waterAttachmentText;
    public int waterAttachmentCost = 10; 
    public bool hasWaterAttachment = false;

    [Header("Water Resistance Upgrade")]
    public Button waterResistanceButton;
    public TextMeshProUGUI waterResistanceText;
    public int waterResistanceCost = 8;
    public int waterResistanceLevel = 0;
    public int maxWaterResistanceLevel = 5;

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

        if (waterAttachmentButton != null)
            waterAttachmentButton.onClick.AddListener(BuyWaterAttachment);

        if (waterResistanceButton != null)
            waterResistanceButton.onClick.AddListener(BuyWaterResistance);

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
        speedUpgradeCost = PlayerPrefs.GetInt("SpeedCost", 5);

        batteryUpgradeLevel = PlayerPrefs.GetInt("BatteryLevel", 0);
        batteryUpgradeCost = PlayerPrefs.GetInt("BatteryCost", 5);

        hasWaterAttachment = PlayerPrefs.GetInt("HasWaterAttachment", 0) == 1;

        waterResistanceLevel = PlayerPrefs.GetInt("WaterResistanceLevel", 0);
        waterResistanceCost = PlayerPrefs.GetInt("WaterResistanceCost", 8);

        Debug.Log("Loaded upgrades - Speed: " + speedUpgradeLevel +
                  ", Battery: " + batteryUpgradeLevel +
                  ", Water: " + hasWaterAttachment +
                  ", Resistance: " + waterResistanceLevel);
    }

    void SaveUpgradeData()
    {
        PlayerPrefs.SetInt("SpeedLevel", speedUpgradeLevel);
        PlayerPrefs.SetInt("SpeedCost", speedUpgradeCost);

        PlayerPrefs.SetInt("BatteryLevel", batteryUpgradeLevel);
        PlayerPrefs.SetInt("BatteryCost", batteryUpgradeCost);

        PlayerPrefs.SetInt("HasWaterAttachment", hasWaterAttachment ? 1 : 0);

        PlayerPrefs.SetInt("WaterResistanceLevel", waterResistanceLevel);
        PlayerPrefs.SetInt("WaterResistanceCost", waterResistanceCost);

        PlayerPrefs.Save();
    }

    void ApplyUpgradesToRoomba()
    {
        if (roombaController == null) return;
        float baseSpeed = 5f;
        roombaController.moveSpeed = baseSpeed + (speedUpgradeLevel * speedUpgradeAmount);

        float baseBattery = 100f;
        roombaController.maxBattery = baseBattery + (batteryUpgradeLevel * batteryUpgradeAmount);

        if (hasWaterAttachment)
        {
            roombaController.ShowWaterTank();
        }

        roombaController.waterResistanceLevel = waterResistanceLevel;

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

    void BuyWaterAttachment()
    {
        if (hasWaterAttachment)
        {
            Debug.Log("Already have water attachment!");
            return;
        }

        if (gameManager.SpendBotcoins(waterAttachmentCost))
        {
            if (audioSource != null && purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            hasWaterAttachment = true;

            if (roombaController != null)
            {
                roombaController.ShowWaterTank();
                roombaController.ClearWaterBurden();
                Debug.Log("Water attachment purchased - burden cleared!");
            }

            SaveUpgradeData(); 
            UpdateUpgradeUI();
        }
        else
        {
            Debug.Log("Not enough money! Need $" + waterAttachmentCost);
        }
    }

    void BuyWaterResistance()
    {
        if (waterResistanceLevel >= maxWaterResistanceLevel)
        {
            Debug.Log("Water resistance maxed out!");
            return;
        }

        if (gameManager.SpendBotcoins(waterResistanceCost))
        {
            if (audioSource != null && purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            waterResistanceLevel++;

            if (roombaController != null)
            {
                roombaController.waterResistanceLevel = waterResistanceLevel;
            }

            waterResistanceCost += 5;

            SaveUpgradeData();
            UpdateUpgradeUI();

            Debug.Log("Water resistance upgraded! Level: " + waterResistanceLevel);
        }
        else
        {
            Debug.Log("Not enough money! Need $" + waterResistanceCost);
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

        if (waterAttachmentButton != null)
        {
            if (hasWaterAttachment)
            {
                waterAttachmentText.text = "+WATER VAC\nPURCHASED";
                waterAttachmentButton.interactable = false;
            }
            else
            {
                waterAttachmentText.text = "+WATER VAC\n$" + waterAttachmentCost;
                waterAttachmentButton.interactable = currentCash >= waterAttachmentCost;
            }
        }

        if (waterResistanceButton != null)
        {
            if (waterResistanceLevel >= maxWaterResistanceLevel)
            {
                waterResistanceText.text = "+WATER RESIST\nMAX LEVEL";
                waterResistanceButton.interactable = false;
            }
            else
            {
                waterResistanceText.text = "+WATER RESIST\n$" + waterResistanceCost +
                                          "\nLevel: " + waterResistanceLevel + "/" + maxWaterResistanceLevel;
                waterResistanceButton.interactable = currentCash >= waterResistanceCost;
            }
        }
    }
}