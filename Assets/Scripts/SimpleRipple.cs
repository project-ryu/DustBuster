using UnityEngine;

public class SimpleRipple : MonoBehaviour
{
    [Header("Ripple Settings")]
    public float expandSpeed = 2f; // Start slower
    public float fadeSpeed = 1f; // Fade slower
    public float maxSize = 2.5f;
    public float startDelay = 0.1f; // Small delay before expanding

    private Material mat;
    private float timer = 0f;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            mat = renderer.material;
            Debug.Log("Ripple material found: " + mat.name);
        }
        else
        {
            Debug.LogError("Ripple has no renderer!");
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Small delay before starting
        if (timer < startDelay)
            return;

        // Expand horizontally
        if (transform.localScale.x < maxSize)
        {
            float growth = expandSpeed * Time.deltaTime;
            transform.localScale += new Vector3(growth, 0, growth);
        }

        // Fade out
        if (mat != null)
        {
            Color color = mat.color;
            color.a -= fadeSpeed * Time.deltaTime;
            mat.color = color;

            // Destroy when fully transparent
            if (color.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}