using UnityEngine;
using UnityEngine.UI;

public class BeaconVisibilityButton : MonoBehaviour
{
    public ImageUploader uploader;
    private Button button;
    private Text label;

    void Start()
    {
        if (uploader == null) uploader = FindObjectOfType<ImageUploader>();
        button = GetComponent<Button>();
        label = GetComponentInChildren<Text>();
        
        if (button != null)
        {
            button.onClick.RemoveAllListeners(); // Clear any lingering listeners
            button.onClick.AddListener(OnClick);
            Debug.Log("[BeaconBtn] Listener added for BeaconToggle");
        }
        else
        {
            Debug.LogError("[BeaconBtn] No Button component found!");
        }
        
        if (uploader == null)
        {
            Debug.LogError("[BeaconBtn] ImageUploader not found!");
        }
        else
        {
            Debug.Log("[BeaconBtn] Initialized successfully");
        }
        
        UpdateVisual();
    }

    void OnClick()
    {
        if (uploader != null)
        {
            uploader.ToggleBeaconVisibility();
            UpdateVisual();
        }
    }

    public void UpdateVisual()
    {
        if (uploader == null || label == null || button == null) return;
        
        bool isOn = uploader.isBeaconVisible;
        label.text = isOn ? "ビーコン: オン" : "ビーコン: オフ";
        
        // Color feedback (Green for ON, Grey for OFF)
        ColorBlock colors = button.colors;
        colors.normalColor = isOn ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.4f, 0.4f, 0.4f);
        colors.highlightedColor = isOn ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
        button.colors = colors;
    }
}
