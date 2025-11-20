using UnityEngine;
using TMPro;

public class MoneyPopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float floatSpeed = 100f; // Screen space pixels per second
    public float fadeSpeed = 1f;
    public float lifetime = 1.5f;
    
    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private float timer = 0f;
    private Color startColor;
    private Vector2 startPosition;
    
    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        
        if (textMesh != null)
        {
            startColor = textMesh.color;
        }
        
        if (rectTransform != null)
        {
            startPosition = rectTransform.anchoredPosition;
        }
    }
    
    void Update()
    {
        // Float upward in screen space
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition + Vector2.up * floatSpeed * timer;
        }
        
        // Fade out
        timer += Time.deltaTime;
        
        if (textMesh != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }
        
        // Destroy when lifetime is over
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    public void SetAmount(int amount)
    {
        if (textMesh != null)
        {
            textMesh.text = "+$" + amount;
        }
    }
    
    public void SetWorldPosition(Vector3 worldPosition)
    {
        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        
        if (rectTransform != null)
        {
            rectTransform.position = screenPos;
            startPosition = rectTransform.anchoredPosition;
        }
    }
}