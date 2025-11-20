using UnityEngine;
using UnityEngine.UI;

public class ShopButtonGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    public float pulseSpeed = 2f;
    public float scaleAmount = 1.15f;
    public Color glowColor = Color.yellow;

    private Image buttonImage;
    private Vector3 originalScale;
    private Color originalColor;
    private bool hasBeenOpened = false;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        originalScale = transform.localScale;

        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
    }

    void Update()
    {
        if (!hasBeenOpened)
        {
            float scale = 1f + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * (scaleAmount - 1f);
            transform.localScale = originalScale * scale;

            if (buttonImage != null)
            {
                float colorLerp = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
                buttonImage.color = Color.Lerp(originalColor, glowColor, colorLerp * 0.5f);
            }
        }
        else
        {
            transform.localScale = originalScale;
            if (buttonImage != null)
            {
                buttonImage.color = originalColor;
            }
        }
    }

    public void OnShopOpened()
    {
        hasBeenOpened = true;
        transform.localScale = originalScale;

        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
    }
}