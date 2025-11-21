using UnityEngine;
using System.Collections.Generic;

public class LaserScanner : MonoBehaviour
{
    [Header("Scanner Settings")]
    public float scanRange = 5f; // Current scan range
    public float scanInterval = 0.2f; // How often to scan (in seconds)
    public LayerMask dustLayer; // Optional: assign if dust is on specific layer
    
    [Header("Visual Indicator")]
    public GameObject scanRangeIndicator; // Optional visual ring
    public bool showScanRange = true;
    
    [Header("Upgrade Levels")]
    public float[] scanRangeLevels = new float[] { 3f, 6f, 10f }; // Levels 1, 2, 3
    public int currentLevel = 0; // 0 = not purchased, 1-3 = upgrade levels
    
    private float scanTimer;
    private List<GameObject> currentlyHighlightedDust = new List<GameObject>();
    
    void Start()
    {
        // Set initial range based on level
        if (currentLevel > 0 && currentLevel <= scanRangeLevels.Length)
        {
            scanRange = scanRangeLevels[currentLevel - 1];
        }
        
        // Create scan range indicator if it doesn't exist
        if (scanRangeIndicator == null && showScanRange)
        {
            CreateScanRangeIndicator();
        }
        
        UpdateScanRangeVisual();
    }
    
    void Update()
    {
        // Only scan if scanner is purchased (level > 0)
        if (currentLevel == 0) return;
        
        scanTimer += Time.deltaTime;
        
        if (scanTimer >= scanInterval)
        {
            ScanForDust();
            scanTimer = 0f;
        }
    }
    
    void ScanForDust()
    {
        // Clear previous highlights
        foreach (GameObject dust in currentlyHighlightedDust)
        {
            if (dust != null)
            {
                DustHighlighter highlighter = dust.GetComponent<DustHighlighter>();
                if (highlighter != null)
                {
                    highlighter.RemoveHighlight();
                }
            }
        }
        currentlyHighlightedDust.Clear();
        
        // Find all dust objects in range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, scanRange);
        
        foreach (Collider col in hitColliders)
        {
            // Check if it's dust (by tag or layer)
            if (col.CompareTag("DustTile"))
            {
                DustHighlighter highlighter = col.GetComponent<DustHighlighter>();
                
                // Add highlighter component if it doesn't exist
                if (highlighter == null)
                {
                    highlighter = col.gameObject.AddComponent<DustHighlighter>();
                }
                
                highlighter.Highlight();
                currentlyHighlightedDust.Add(col.gameObject);
            }
        }
    }
    
    public void UpgradeScanner()
    {
        if (currentLevel < scanRangeLevels.Length)
        {
            currentLevel++;
            scanRange = scanRangeLevels[currentLevel - 1];
            UpdateScanRangeVisual();
            Debug.Log($"Scanner upgraded to level {currentLevel}! Range: {scanRange}m");
        }
    }
    
    public bool IsMaxLevel()
    {
        return currentLevel >= scanRangeLevels.Length;
    }
    
    public void SetScannerLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, scanRangeLevels.Length);
        if (currentLevel > 0)
        {
            scanRange = scanRangeLevels[currentLevel - 1];
            UpdateScanRangeVisual();
        }
    }
    
    void CreateScanRangeIndicator()
    {
        // Create a simple cylinder ring to show scan range
        scanRangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scanRangeIndicator.name = "ScanRangeIndicator";
        scanRangeIndicator.transform.SetParent(transform);
        scanRangeIndicator.transform.localPosition = Vector3.zero;
        
        // Make it a flat ring
        scanRangeIndicator.transform.localScale = new Vector3(scanRange * 2, 0.01f, scanRange * 2);
        
        // Remove collider so it doesn't interfere
        Destroy(scanRangeIndicator.GetComponent<Collider>());
        
        // Create semi-transparent material
        Material ringMat = new Material(Shader.Find("Standard"));
        ringMat.color = new Color(0f, 1f, 1f, 0.2f); // Cyan, semi-transparent
        ringMat.SetFloat("_Mode", 3); // Transparent mode
        ringMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ringMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ringMat.SetInt("_ZWrite", 0);
        ringMat.DisableKeyword("_ALPHATEST_ON");
        ringMat.EnableKeyword("_ALPHABLEND_ON");
        ringMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ringMat.renderQueue = 3000;
        
        scanRangeIndicator.GetComponent<Renderer>().material = ringMat;
    }
    
    void UpdateScanRangeVisual()
    {
        if (scanRangeIndicator != null)
        {
            scanRangeIndicator.transform.localScale = new Vector3(scanRange * 2, 0.01f, scanRange * 2);
            scanRangeIndicator.SetActive(currentLevel > 0 && showScanRange);
        }
    }
    
    public void ToggleScanRangeVisual(bool show)
    {
        showScanRange = show;
        if (scanRangeIndicator != null)
        {
            scanRangeIndicator.SetActive(show && currentLevel > 0);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw scan range in editor
        if (currentLevel > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, scanRange);
        }
    }
}