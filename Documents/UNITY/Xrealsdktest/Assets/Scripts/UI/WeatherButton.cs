using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for IPointerEnterHandler
using NRKernal;

/// <summary>
/// Runtime click handler for Weather Button
/// Uses Button.onClick pattern (like BottomMenuController) for reliable NRSDK compatibility
/// </summary>
public class WeatherButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button btn;
    private ImageUploader uploader;
    public WeatherManager weatherMgr; // Made public for assignment
    
    // NRSDK Input Support
    private bool _isHovering = false;
    private bool _wasPressed = false;
    private Vector3 _originalScale;

    void Start()
    {
        _originalScale = transform.localScale;

        // Get Button component - Keep for fallback, but we primarily use Update loop
        btn = GetComponent<Button>();
        
        // Cache references if not assigned
        if (weatherMgr == null)
        {
            uploader = FindObjectOfType<ImageUploader>();
            if (uploader != null)
            {
                weatherMgr = uploader.GetComponent<WeatherManager>();
            }
            if (weatherMgr == null) weatherMgr = FindObjectOfType<WeatherManager>();
        }
        
        Debug.Log($"[WeatherButton] Initialized. WeatherManager: {(weatherMgr != null ? "Found" : "NOT FOUND")}");
    }

    void Update()
    {
        // Direct input detection when hovering (Robust for NRSDK)
        if (_isHovering)
        {
            bool isPressed = false;
            
            // Check NRInput for controller trigger
            #if !UNITY_EDITOR
            try { isPressed = NRInput.GetButtonDown(ControllerButton.TRIGGER); }
            catch { isPressed = Input.GetMouseButtonDown(0); }
            #else
            isPressed = Input.GetMouseButtonDown(0);
            #endif
            
            if (isPressed && !_wasPressed)
            {
                OnButtonClick();
            }
            
            _wasPressed = isPressed;
        }
        else
        {
            _wasPressed = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        transform.localScale = _originalScale * 1.2f; // Scale up effect
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        transform.localScale = _originalScale; // Reset scale
    }

    void OnButtonClick()
    {
        Debug.Log("[WeatherButton] OnButtonClick triggered!");
        
        if (weatherMgr != null)
        {
            Debug.Log("[WeatherButton] Calling ShowWeatherPanel...");
            weatherMgr.ShowWeatherPanel();
        }
        else
        {
            // Try to find again in case it was created after Start
            weatherMgr = FindObjectOfType<WeatherManager>();
            if (weatherMgr != null)
            {
                weatherMgr.ShowWeatherPanel();
            }
            else
            {
                Debug.LogError("[WeatherButton] WeatherManager not found anywhere!");
            }
        }
    }
}
