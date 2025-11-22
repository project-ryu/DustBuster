using UnityEngine;
using System.Collections.Generic;

public class LaserScanner : MonoBehaviour
{
    [Header("Scanner Settings")]
    public float scanRange = 15f;
    public float scanInterval = 2f;
    public LayerMask dustLayer;
    
    [Header("Ring Wave Visual")]
    public float ringThickness = 0.5f; // How thick the ring is
    public float waveExpandSpeed = 20f;
    public Color ringColor = Color.cyan;
    public float ringEmissionIntensity = 2f;
    public int ringSegments = 64; // Ring detail
    
    [Header("Upgrade Levels")]
    public float[] scanRangeLevels = new float[] { 15f, 30f, 50f };
    public int currentLevel = 0;
    
    [Header("Dust Highlight Settings")]
    public float highlightDuration = 0.5f; // Dust highlighted briefly as ring passes
    
    private float scanTimer;
    private HashSet<GameObject> processedDust = new HashSet<GameObject>();
    
    void Start()
    {
        // If currentLevel is set, use the level's range
        if (currentLevel > 0 && currentLevel <= scanRangeLevels.Length)
        {
            scanRange = scanRangeLevels[currentLevel - 1];
            Debug.Log($"Scanner initialized at Level {currentLevel} with range: {scanRange}m");
        }
        else
        {
            Debug.Log($"Scanner using manual range: {scanRange}m (Level: {currentLevel})");
        }
        
        // Send first pulse immediately if scanner is active
        if (currentLevel > 0)
        {
            SendSonarPulse();
        }
    }
    
    void Update()
    {
        if (currentLevel == 0) return;
        
        scanTimer += Time.deltaTime;
        
        if (scanTimer >= scanInterval)
        {
            SendSonarPulse();
            scanTimer = 0f;
        }
    }
    
    void SendSonarPulse()
    {
        Debug.Log($"🔊 SONAR PULSE! Scanner Level: {currentLevel}, Range: {scanRange}m");
        
        // Create ring wave
        GameObject ringWave = new GameObject("SonarRing");
        ringWave.transform.position = transform.position;
        
        SonarRingWave waveScript = ringWave.AddComponent<SonarRingWave>();
        waveScript.Initialize(
            scanRange,  // Make sure this is the right value
            waveExpandSpeed, 
            ringThickness, 
            ringColor, 
            ringEmissionIntensity,
            ringSegments,
            highlightDuration
        );
        
        Debug.Log($"Ring created with maxRadius: {scanRange}");
    }
    
    public void UpgradeScanner()
    {
        if (currentLevel < scanRangeLevels.Length)
        {
            currentLevel++;
            scanRange = scanRangeLevels[currentLevel - 1];
            Debug.Log($"Scanner upgraded to level {currentLevel}! Range: {scanRange}m");
        }
    }
    
    public bool IsMaxLevel()
    {
        return currentLevel >= scanRangeLevels.Length;
    }
    
    public void SetScannerLevel(int level)
    {
        int previousLevel = currentLevel;
        currentLevel = Mathf.Clamp(level, 0, scanRangeLevels.Length);
        
        if (currentLevel > 0)
        {
            scanRange = scanRangeLevels[currentLevel - 1];
            Debug.Log($"Scanner level set to {currentLevel}, range: {scanRange}m");
            
            // Send immediate pulse when first activated or upgraded
            if (previousLevel == 0)
            {
                Debug.Log("Scanner activated for first time - sending immediate pulse!");
                SendSonarPulse();
                scanTimer = 0f; // Reset timer so next pulse happens at correct interval
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (currentLevel > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, scanRange);
        }
    }
}

public class SonarRingWave : MonoBehaviour
{
    private float maxRadius;
    private float expandSpeed;
    private float ringThickness;
    private float currentRadius = 0f;
    private float highlightDuration;
    
    private HashSet<GameObject> highlightedDust = new HashSet<GameObject>();
    
    // Track highlights with individual timers
    private class HighlightTimer
    {
        public DustHighlighter highlighter;
        public float timeRemaining;
    }
    private List<HighlightTimer> activeHighlights = new List<HighlightTimer>();
    
    public void Initialize(float maxRad, float speed, float thickness, Color color, float emission, int segs, float highlightDur)
    {
        maxRadius = maxRad;
        expandSpeed = speed;
        ringThickness = thickness;
        highlightDuration = highlightDur;
    }
    
    void Update()
    {
        // Only expand if we haven't reached max radius yet
        if (currentRadius < maxRadius)
        {
            currentRadius += expandSpeed * Time.deltaTime;
            
            // Clamp to max (don't overshoot)
            currentRadius = Mathf.Min(currentRadius, maxRadius);
            
            // Only scan while expanding
            ScanAtCurrentRadius();
            
            // Debug every 30 frames
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"Ring expanding: {currentRadius:F1}m / {maxRadius:F1}m");
            }
        }
        
        // Always update highlight timers
        UpdateHighlightTimers();
        
        // Destroy when reached max radius and all highlights are done
        if (currentRadius >= maxRadius && activeHighlights.Count == 0)
        {
            Debug.Log($"Ring complete at {maxRadius}m - destroying");
            Destroy(gameObject);
        }
    }
    
    void ScanAtCurrentRadius()
    {
        Vector3 center = transform.position;
        center.y = 0;
        
        Collider[] hitColliders = Physics.OverlapSphere(center, currentRadius + ringThickness);
        
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("DustTile"))
            {
                if (highlightedDust.Contains(col.gameObject))
                    continue;
                
                Vector3 dustPos = col.transform.position;
                dustPos.y = 0;
                float distance = Vector3.Distance(center, dustPos);
                
                if (Mathf.Abs(distance - currentRadius) <= ringThickness)
                {
                    HighlightDust(col.gameObject);
                    highlightedDust.Add(col.gameObject);
                }
            }
        }
    }
    
    void HighlightDust(GameObject dust)
    {
        DustHighlighter highlighter = dust.GetComponent<DustHighlighter>();
        
        if (highlighter == null)
        {
            highlighter = dust.gameObject.AddComponent<DustHighlighter>();
        }
        
        highlighter.Highlight();
        
        // Add to timer list
        activeHighlights.Add(new HighlightTimer
        {
            highlighter = highlighter,
            timeRemaining = highlightDuration
        });
    }
    
    void UpdateHighlightTimers()
    {
        // Count down all active highlights
        for (int i = activeHighlights.Count - 1; i >= 0; i--)
        {
            HighlightTimer timer = activeHighlights[i];
            timer.timeRemaining -= Time.deltaTime;
            
            if (timer.timeRemaining <= 0)
            {
                // Time expired - remove highlight
                if (timer.highlighter != null)
                {
                    timer.highlighter.RemoveHighlight();
                }
                activeHighlights.RemoveAt(i);
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up any remaining highlights
        foreach (var timer in activeHighlights)
        {
            if (timer.highlighter != null)
            {
                timer.highlighter.RemoveHighlight();
            }
        }
    }
}
