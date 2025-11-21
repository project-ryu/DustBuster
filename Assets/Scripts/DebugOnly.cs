using UnityEngine;

public class DebugOnly : MonoBehaviour
{
    void Start()
    {
#if !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }
}