using UnityEngine;

public class DustHighlighter : MonoBehaviour
{
    private Material originalMaterial;
    private Material highlightMaterial;
    private Renderer objectRenderer;
    private bool isHighlighted = false;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            
            // Create a glowing highlight material
            highlightMaterial = new Material(originalMaterial);
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", Color.blue * 2f);
        }
    }

    public void Highlight()
    {
        if (!isHighlighted && objectRenderer != null)
        {
            objectRenderer.material = highlightMaterial;
            isHighlighted = true;
        }
    }

    public void RemoveHighlight()
    {
        if (isHighlighted && objectRenderer != null)
        {
            objectRenderer.material = originalMaterial;
            isHighlighted = false;
        }
    }

    void OnDestroy()
    {
        // Clean up materials
        if (highlightMaterial != null)
        {
            Destroy(highlightMaterial);
        }
    }
}