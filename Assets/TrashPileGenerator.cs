using UnityEngine;

public class TrashPileGenerator : MonoBehaviour
{
    [Header("Pile Settings")]
    public int numberOfPieces = 8;
    public Vector3 minSize = new Vector3(0.15f, 0.15f, 0.15f);
    public Vector3 maxSize = new Vector3(0.4f, 0.4f, 0.4f);
    public float pileRadius = 0.3f;
    public float maxHeight = 0.6f;
    
    [Header("Visual Settings")]
    public Material[] trashMaterials;
    public bool randomRotation = true;
    
    [Header("Debug")]
    public bool generateOnStart = true;
    
    void Start()
    {
        if (generateOnStart)
        {
            Debug.Log("TrashPileGenerator Start() called - generating pile...");
            GeneratePile();
        }
    }
    
    // Manual button in Inspector to generate pile
    [ContextMenu("Generate Pile Now")]
    public void GeneratePile()
    {
        Debug.Log("=== GENERATING TRASH PILE ===");
        Debug.Log("Number of pieces: " + numberOfPieces);
        
        // Clear any existing pieces first
        ClearPile();
        
        for (int i = 0; i < numberOfPieces; i++)
        {
            // Create a cube
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "TrashPiece_" + i;
            
            Debug.Log("Created piece " + i + ": " + piece.name);
            
            // Make it a child of this object
            piece.transform.SetParent(transform);
            
            // Random size
            Vector3 size = new Vector3(
                Random.Range(minSize.x, maxSize.x),
                Random.Range(minSize.y, maxSize.y),
                Random.Range(minSize.z, maxSize.z)
            );
            piece.transform.localScale = size;
            
            // Random position within pile radius
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, pileRadius);
            float height = Random.Range(0f, maxHeight);
            
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * distance,
                height,
                Mathf.Sin(angle) * distance
            );
            piece.transform.localPosition = localPos;
            
            Debug.Log("Piece " + i + " position: " + localPos);
            
            // Random rotation
            if (randomRotation)
            {
                piece.transform.localRotation = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );
            }
            
            // Apply material/color
            Renderer renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (trashMaterials != null && trashMaterials.Length > 0)
                {
                    renderer.material = trashMaterials[Random.Range(0, trashMaterials.Length)];
                    Debug.Log("Applied material to piece " + i);
                }
                else
                {
                    // Create a default brown/gray material if none assigned
                    Material defaultMat = new Material(Shader.Find("Standard"));
                    defaultMat.color = new Color(
                        Random.Range(0.3f, 0.5f), 
                        Random.Range(0.3f, 0.4f), 
                        Random.Range(0.2f, 0.3f)
                    );
                    renderer.material = defaultMat;
                    Debug.Log("Applied default material to piece " + i);
                }
            }
            
            // Remove the individual collider (parent has the trigger collider)
            Collider pieceCollider = piece.GetComponent<Collider>();
            if (pieceCollider != null)
            {
                Destroy(pieceCollider);
            }
        }
        
        Debug.Log("=== PILE GENERATION COMPLETE ===");
        Debug.Log("Total children: " + transform.childCount);
    }
    
    [ContextMenu("Clear Pile")]
    public void ClearPile()
    {
        Debug.Log("Clearing existing pile pieces...");
        
        // Destroy all children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
        
        Debug.Log("Pile cleared");
    }
}