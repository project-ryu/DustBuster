using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Transform roombaTransform;
    
    [Header("Normal View Settings")]
    public Vector3 normalPositionOffset = new Vector3(0, 8, -10);
    public Vector3 normalRotationOffset = new Vector3(30, 0, 0);
    
    [Header("Reverse View Settings")]
    public KeyCode reverseKey = KeyCode.R; // Hotkey for reverse camera
    public Vector3 reversePositionOffset = new Vector3(0, 8, 10); // In front looking back
    public Vector3 reverseRotationOffset = new Vector3(30, 180, 0); // Looking backward
    
    [Header("Bird's Eye View Settings")]
    public Vector3 birdsEyePosition = new Vector3(0, 25, 0);
    public float birdsEyeRotationX = 90f;
    public float orthographicSize = 15f;
    
    [Header("Transition Settings")]
    public float transitionDuration = 2f;
    public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Normal Following")]
    public float followSpeed = 15f;
    public float rotationSpeed = 15f;
    public float reverseTransitionSpeed = 10f; // How fast to flip to reverse view
    
    private bool isBirdsEyeView = false;
    private bool isReverseView = false;
    private Camera cam;
    private float originalFieldOfView;
    
    private bool isTransitioning = false;
    private float transitionProgress = 0f;
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (cam != null)
        {
            originalFieldOfView = cam.fieldOfView;
        }
        
        if (transform.parent != null)
        {
            roombaTransform = transform.parent;
            transform.SetParent(null);
        }
        else
        {
            if (roombaTransform == null)
            {
                GameObject roomba = GameObject.FindGameObjectWithTag("Player");
                if (roomba != null)
                {
                    roombaTransform = roomba.transform;
                }
            }
        }
        
        if (roombaTransform != null)
        {
            Vector3 worldOffset = roombaTransform.TransformDirection(normalPositionOffset);
            transform.position = roombaTransform.position + worldOffset;
            transform.rotation = roombaTransform.rotation * Quaternion.Euler(normalRotationOffset);
        }

        cam = GetComponent<Camera>();
    
        if (cam != null)
        {
            // Increase near clip to prevent seeing inside objects
            cam.nearClipPlane = 0.5f; // Increase from default 0.3
            Debug.Log("Camera near clip plane set to: " + cam.nearClipPlane);
        }
    }

    void LateUpdate()
    {
        if (roombaTransform == null) return;
        
        // Check for reverse camera toggle (only when not in bird's eye view)
        if (!isBirdsEyeView)
        {
            if (Input.GetKeyDown(reverseKey)) // Changed from GetKey to GetKeyDown
            {
                isReverseView = !isReverseView; // Toggle on/off
            }
        }
        else
        {
            // Turn off reverse view when entering bird's eye mode
            isReverseView = false;
        }
        
        if (isTransitioning)
        {
            UpdateTransition();
        }
        else if (isBirdsEyeView)
        {
            MaintainBirdsEyeView();
        }
        else
        {
            UpdateNormalFollow();
        }
    }

    void UpdateTransition()
    {
        transitionProgress += Time.deltaTime / transitionDuration;
        
        if (transitionProgress >= 1f)
        {
            transitionProgress = 1f;
            isTransitioning = false;
            
            if (isBirdsEyeView)
            {
                transform.position = birdsEyePosition;
                transform.rotation = Quaternion.Euler(birdsEyeRotationX, 0, 0);
            }
        }
        else
        {
            float posEase = positionCurve.Evaluate(transitionProgress);
            float rotEase = rotationCurve.Evaluate(transitionProgress);
            
            if (isBirdsEyeView)
            {
                Vector3 targetPos = birdsEyePosition;
                Quaternion targetRot = Quaternion.Euler(birdsEyeRotationX, 0, 0);
                
                transform.position = Vector3.Lerp(transitionStartPosition, targetPos, posEase);
                transform.rotation = Quaternion.Slerp(transitionStartRotation, targetRot, rotEase);
            }
            else
            {
                Vector3 worldOffset = roombaTransform.TransformDirection(normalPositionOffset);
                Vector3 targetPos = roombaTransform.position + worldOffset;
                Quaternion targetRot = roombaTransform.rotation * Quaternion.Euler(normalRotationOffset);
                
                transform.position = Vector3.Lerp(transitionStartPosition, targetPos, posEase);
                transform.rotation = Quaternion.Slerp(transitionStartRotation, targetRot, rotEase);
            }
        }
    }

    void MaintainBirdsEyeView()
    {
        transform.position = birdsEyePosition;
        transform.rotation = Quaternion.Euler(birdsEyeRotationX, 0, 0);
        
        if (cam != null && !cam.orthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }
    }

    void UpdateNormalFollow()
    {
        if (cam != null && cam.orthographic)
        {
            cam.orthographic = false;
            cam.fieldOfView = originalFieldOfView;
        }
        
        Vector3 currentOffset = isReverseView ? reversePositionOffset : normalPositionOffset;
        Vector3 currentRotationOffset = isReverseView ? reverseRotationOffset : normalRotationOffset;
        
        Vector3 worldOffset = roombaTransform.TransformDirection(currentOffset);
        Vector3 targetPosition = roombaTransform.position + worldOffset;
        Quaternion targetRotation = roombaTransform.rotation * Quaternion.Euler(currentRotationOffset);
        
        float speed = isReverseView ? reverseTransitionSpeed : followSpeed;
        float rotSpeed = isReverseView ? reverseTransitionSpeed : rotationSpeed;
        
        float posSmoothing = Mathf.Min(speed * Time.deltaTime, 1f);
        float rotSmoothing = Mathf.Min(rotSpeed * Time.deltaTime, 1f);
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, posSmoothing);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSmoothing);
    }

    public void SetBirdsEyeView(bool enabled)
    {
        if (isBirdsEyeView != enabled)
        {
            isBirdsEyeView = enabled;
            isTransitioning = true;
            transitionProgress = 0f;
            
            transitionStartPosition = transform.position;
            transitionStartRotation = transform.rotation;
            
            // Switch projection immediately
            if (cam != null)
            {
                if (enabled)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = orthographicSize;
                }
                else
                {
                    cam.orthographic = false;
                    cam.fieldOfView = originalFieldOfView;
                }
            }
        }
    }
}