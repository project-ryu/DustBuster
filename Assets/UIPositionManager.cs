using UnityEngine;

public class UIPositionManager : MonoBehaviour
{
    [Header("UI Elements to Move")]
    public RectTransform batteryGauge;
    public RectTransform tilesCounter;
    
    [Header("Normal Positions")]
    public Vector2 batteryNormalPosition;
    public Vector2 tilesNormalPosition;
    
    [Header("Bird's Eye Positions")]
    public Vector2 batteryBirdsEyePosition = new Vector2(-50, -50); // Bottom-left
    public Vector2 tilesBirdsEyePosition = new Vector2(50, 50); // Top-right
    
    [Header("Transition Settings")]
    public float transitionSpeed = 5f;
    
    private bool isBirdsEyeMode = false;
    private Vector2 batteryTargetPosition;
    private Vector2 tilesTargetPosition;

    void Start()
    {
        // Store original positions
        if (batteryGauge != null)
        {
            batteryNormalPosition = batteryGauge.anchoredPosition;
        }
        
        if (tilesCounter != null)
        {
            tilesNormalPosition = tilesCounter.anchoredPosition;
        }
        
        // Set initial targets to normal positions
        batteryTargetPosition = batteryNormalPosition;
        tilesTargetPosition = tilesNormalPosition;
    }

    void Update()
    {
        // Smoothly move UI elements to target positions
        if (batteryGauge != null)
        {
            batteryGauge.anchoredPosition = Vector2.Lerp(
                batteryGauge.anchoredPosition, 
                batteryTargetPosition, 
                Time.deltaTime * transitionSpeed
            );
        }
        
        if (tilesCounter != null)
        {
            tilesCounter.anchoredPosition = Vector2.Lerp(
                tilesCounter.anchoredPosition, 
                tilesTargetPosition, 
                Time.deltaTime * transitionSpeed
            );
        }
    }

    public void SetBirdsEyeMode(bool enabled)
    {
        isBirdsEyeMode = enabled;
        
        if (enabled)
        {
            // Move to bird's eye positions (sides)
            batteryTargetPosition = batteryBirdsEyePosition;
            tilesTargetPosition = tilesBirdsEyePosition;
        }
        else
        {
            // Return to normal positions
            batteryTargetPosition = batteryNormalPosition;
            tilesTargetPosition = tilesNormalPosition;
        }
    }
}