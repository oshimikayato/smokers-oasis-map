using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NRKernal;

/// <summary>
/// Standalone Weather Widget Controller
/// Uses direct raycast-based click detection instead of relying on Button.onClick
/// </summary>
public class WeatherWidgetController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Text weatherDisplayText;
    public Image backgroundImage;
    
    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.3f, 0.7f, 1f, 0.9f);
    public Color hoverColor = new Color(0.5f, 0.85f, 1f, 1f);
    public Color pressedColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    
    private bool _isHovering = false;
    private bool _wasPressed = false;
    private WeatherManager _weatherManager;
    private Vector3 _originalScale;

    void Start()
    {
        _originalScale = transform.localScale;
        
        // Find WeatherManager
        _weatherManager = FindObjectOfType<WeatherManager>();
        if (_weatherManager == null)
        {
            // Try to find via ImageUploader
            var uploader = FindObjectOfType<ImageUploader>();
            if (uploader != null)
            {
                _weatherManager = uploader.GetComponent<WeatherManager>();
            }
        }
        
        Debug.Log($"[WeatherWidget] Initialized. WeatherManager: {(_weatherManager != null ? "Found" : "NOT FOUND")}");
        
        // Ensure background exists
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        
        // Ensure text exists
        if (weatherDisplayText == null)
        {
            weatherDisplayText = GetComponentInChildren<Text>();
        }
        
        // Set initial color
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    void Update()
    {
        // Direct input detection when hovering
        if (_isHovering)
        {
            bool isPressed = false;
            
            // Check NRInput for controller trigger
            #if !UNITY_EDITOR
            try
            {
                isPressed = NRInput.GetButtonDown(ControllerButton.TRIGGER);
            }
            catch
            {
                // NRInput not available, fallback to mouse
                isPressed = Input.GetMouseButtonDown(0);
            }
            #else
            // Editor: use mouse
            isPressed = Input.GetMouseButtonDown(0);
            #endif
            
            if (isPressed && !_wasPressed)
            {
                OnClick();
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
        Debug.Log("[WeatherWidget] Hover Start");
        
        // Visual feedback
        if (backgroundImage != null)
        {
            backgroundImage.color = hoverColor;
        }
        transform.localScale = _originalScale * 1.15f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        Debug.Log("[WeatherWidget] Hover End");
        
        // Reset visual
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
        transform.localScale = _originalScale;
    }

    void OnClick()
    {
        Debug.Log("[WeatherWidget] CLICK DETECTED!");
        
        // Visual feedback
        if (backgroundImage != null)
        {
            backgroundImage.color = pressedColor;
            Invoke(nameof(ResetColor), 0.1f);
        }
        
        // Open weather panel
        if (_weatherManager != null)
        {
            Debug.Log("[WeatherWidget] Calling ShowWeatherPanel()...");
            _weatherManager.ShowWeatherPanel();
        }
        else
        {
            // Try to find again
            _weatherManager = FindObjectOfType<WeatherManager>();
            if (_weatherManager != null)
            {
                _weatherManager.ShowWeatherPanel();
            }
            else
            {
                Debug.LogError("[WeatherWidget] WeatherManager still not found!");
            }
        }
    }

    void ResetColor()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = _isHovering ? hoverColor : normalColor;
        }
    }

    /// <summary>
    /// Update the weather display text (called by WeatherManager)
    /// </summary>
    public void UpdateDisplay(string text)
    {
        if (weatherDisplayText != null)
        {
            weatherDisplayText.text = text;
        }
    }
}
