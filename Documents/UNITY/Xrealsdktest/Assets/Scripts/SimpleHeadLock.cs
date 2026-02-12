using UnityEngine;

public class SimpleHeadLock : MonoBehaviour
{
    [Tooltip("The camera to lock to. If null, uses Camera.main")]
    public Transform targetCamera;
    
    [Tooltip("Distance from camera in meters")]
    public float distance = 1.0f; // Default 1.0m
    
    [Tooltip("Target scale of the UI")]
    public float targetScale = 0.0006f; // Reduced default (0.6mm per pixel) for better FOV fit

    void Start()
    {
        if (targetCamera == null)
        {
            // Try to find CenterCamera of NRSDK first
            GameObject rig = GameObject.Find("NRCameraRig");
            if (rig != null)
            {
                Transform t = rig.transform.Find("TrackingSpace/CenterCamera");
                if (t != null) targetCamera = t;
            }

            // Fallback to MainCamera
            if (targetCamera == null)
            {
                Camera main = Camera.main;
                if (main != null) targetCamera = main.transform;
            }
        }
    }

    void LateUpdate()
    {
        // 1. Find Camera if needed
        if (targetCamera == null)
        {
            // Method 1: NRCameraRig/TrackingSpace/CenterCamera (by name)
            GameObject rig = GameObject.Find("NRCameraRig");
            if (rig != null)
            {
                Transform t = rig.transform.Find("TrackingSpace/CenterCamera");
                if (t != null) 
                {
                    targetCamera = t;
                    Debug.Log("[SimpleHeadLock] Found CenterCamera via NRCameraRig path");
                }
            }
            
            // Method 1b: NRCameraRig might have (Clone) suffix
            if (targetCamera == null)
            {
                var allRigs = GameObject.FindObjectsOfType<Transform>();
                foreach (var r in allRigs)
                {
                    if (r.name.StartsWith("NRCameraRig"))
                    {
                        Transform t = r.Find("TrackingSpace/CenterCamera");
                        if (t != null)
                        {
                            targetCamera = t;
                            Debug.Log("[SimpleHeadLock] Found CenterCamera via NRCameraRig* search: " + r.name);
                            break;
                        }
                    }
                }
            }
            
            // Method 2: Direct find CenterCamera
            if (targetCamera == null)
            {
                GameObject centerCam = GameObject.Find("CenterCamera");
                if (centerCam != null) 
                {
                    targetCamera = centerCam.transform;
                    Debug.Log("[SimpleHeadLock] Found CenterCamera directly");
                }
            }
            
            // Method 3: Camera.main
            if (targetCamera == null)
            {
                Camera main = Camera.main;
                if (main != null) 
                {
                    targetCamera = main.transform;
                    Debug.Log("[SimpleHeadLock] Using Camera.main: " + main.name);
                }
            }
            
            if (targetCamera == null)
            {
                // Throttle warning to once per second
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning("[SimpleHeadLock] NO CAMERA FOUND! Waiting for NRSDK initialization...");
                }
                return;
            }
        }

        // 2. Lock to Camera (Soft Lock via position/rotation update)
        // DO NOT PARENT: Parenting causes destruction if the camera is destroyed by NRSDK
        if (transform.parent != null) transform.SetParent(null); // Ensure world space

        transform.position = targetCamera.position + targetCamera.forward * distance;
        transform.rotation = targetCamera.rotation;
        transform.localScale = Vector3.one * targetScale;
        
        // CRITICAL: Set Canvas.worldCamera for proper rendering
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            Camera cam = targetCamera.GetComponent<Camera>();
            if (cam != null && canvas.worldCamera != cam)
            {
                canvas.worldCamera = cam;
                Debug.Log("[SimpleHeadLock] Set Canvas.worldCamera to: " + cam.name);
                
                // Ensure camera renders UI layer (Layer 5)
                int uiLayerMask = 1 << 5; 
                if ((cam.cullingMask & uiLayerMask) == 0)
                {
                    cam.cullingMask |= uiLayerMask;
                    Debug.Log("[SimpleHeadLock] Added UI layer to camera culling mask");
                }
            }
        }
    }
}
