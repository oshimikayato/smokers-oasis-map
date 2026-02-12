using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;

public class RegistrationListButton : MonoBehaviour, IPointerClickHandler
{
    public string ObjectName;
    public Image CheckboxImage;
    public Sprite UncheckedSprite;
    public Sprite CheckedSprite;
    public Image BackgroundImage;
    
    private Button _button;
    private ImageUploader _uploader;
    private RectTransform _rectTransform;

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
        
        // Ensure Button doesn't block us if we want to handle clicks, 
        // but usually Button and custom PointerClickHandler can coexist.
        // We add listener just in case Button works.
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners(); 
            _button.onClick.AddListener(OnButtonClick);
            Debug.Log($"[RegListButton] Listener added for {ObjectName}");
        }
        
        // Find ImageUploader
        _uploader = FindObjectOfType<ImageUploader>();
        if (_uploader == null)
        {
            Debug.LogError($"[RegListButton] ImageUploader not found for {ObjectName}");
        }
        else
        {
            UpdateVisuals();
            Debug.Log($"[RegListButton] {ObjectName} initialized successfully");
        }
    }

    void OnEnable()
    {
        UpdateVisuals();
    }
    
    // Direct interface implementation to catch physics raycast events
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[RegListButton] OnPointerClick received for {ObjectName}");
        OnButtonClick();
    }

    public void OnButtonClick()
    {
        if (_uploader == null) _uploader = FindObjectOfType<ImageUploader>();
        
        if (_uploader != null)
        {
            bool success = _uploader.ToggleTargetSelection(ObjectName);
            UpdateVisuals();
            Debug.Log($"[RegListButton] Clicked {ObjectName}. Success: {success}");
            
            // Simple click feedback (flash slightly brighter/different)
            StartCoroutine(ClickEffect());
        }
        else
        {
            Debug.LogError($"[RegListButton] Cannot click {ObjectName} - ImageUploader missing");
        }
    }
    
    private System.Collections.IEnumerator ClickEffect()
    {
        if (BackgroundImage != null)
        {
            Color original = BackgroundImage.color;
            BackgroundImage.color = Color.white; // Flash white
            yield return new WaitForSeconds(0.1f);
            
            // Re-apply correct visual state
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_uploader == null) return;

        bool isSelected = _uploader.IsTargetSelected(ObjectName);
        
        // Update Checkbox
        if (CheckboxImage != null)
        {
            CheckboxImage.sprite = isSelected ? CheckedSprite : UncheckedSprite;
        }

        // Update Background Color directly (Button transition is None)
        if (BackgroundImage != null)
        {
            BackgroundImage.color = isSelected 
                ? new Color(0.1f, 0.4f, 0.2f, 1f) // Selected: Greenish
                : new Color(0.12f, 0.15f, 0.22f, 1f); // Normal: Dark Blueish
        }
    }
}
