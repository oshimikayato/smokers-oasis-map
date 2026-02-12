using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NRKernal;

/// <summary>
/// NRSDK入力の物理レイでUIクリックを補助するフォールバック。
/// EventSystem経由のクリックが失敗するケースに備える。
/// </summary>
public class NRUIRaycastClicker : MonoBehaviour
{
    private const string BuildTag = "NRUIRaycastClicker v2";
    [Header("Raycast Settings")]
    public float maxDistance = 100f;
    public LayerMask hitLayers = ~0;

    [Header("Debug")]
    public bool logHits = true;

    private bool _wasPinching;
    private bool _warnedNoNRInput;
    private bool _loggedStart;
    private bool _warnedInputException;

    void Awake()
    {
        Debug.Log($"[NRUIRaycastClicker] Awake {BuildTag}");
    }

    void OnEnable()
    {
        if (logHits) Debug.Log("[NRUIRaycastClicker] OnEnable");
    }

    void Start()
    {
        if (logHits)
        {
            Debug.Log("[NRUIRaycastClicker] Start");
            _loggedStart = true;
        }
    }

    void Update()
    {
        if (logHits && !_loggedStart)
        {
            Debug.Log("[NRUIRaycastClicker] Update running (Start not logged)");
            _loggedStart = true;
        }
        if (!TryGetInputSource(out InputSourceEnum inputSource))
        {
            if (logHits && !_warnedNoNRInput)
            {
                Debug.LogWarning("[NRUIRaycastClicker] NRInput not initialized. RaycastClicker paused.");
                _warnedNoNRInput = true;
            }
            return;
        }

        // Controller input
        if (inputSource == InputSourceEnum.Controller)
        {
            if (TryGetTriggerDown())
            {
                TryControllerRaycast();
            }
        }
        else
        {
            // Hand tracking input
            HandState rightHand = TryGetHandState(HandEnum.RightHand);
            if (rightHand != null && rightHand.isPinching)
            {
                if (!_wasPinching)
                {
                    _wasPinching = true;
                    TryHandRaycast(rightHand);
                }
            }
            else
            {
                _wasPinching = false;
            }
        }
    }

    private bool TryGetInputSource(out InputSourceEnum inputSource)
    {
        inputSource = InputSourceEnum.Controller;
        try
        {
            if (!NRInput.IsInitialized) return false;
            inputSource = NRInput.CurrentInputSourceType;
            return true;
        }
        catch (System.Exception e)
        {
            if (!_warnedInputException)
            {
                Debug.LogError($"[NRUIRaycastClicker] NRInput exception: {e.GetType().Name} {e.Message}");
                _warnedInputException = true;
            }
            return false;
        }
    }

    private bool TryGetTriggerDown()
    {
        try
        {
            return NRInput.GetButtonDown(ControllerButton.TRIGGER);
        }
        catch (System.Exception e)
        {
            if (!_warnedInputException)
            {
                Debug.LogError($"[NRUIRaycastClicker] NRInput trigger exception: {e.GetType().Name} {e.Message}");
                _warnedInputException = true;
            }
            return false;
        }
    }

    private HandState TryGetHandState(HandEnum hand)
    {
        try
        {
            return NRInput.Hands.GetHandState(hand);
        }
        catch (System.Exception e)
        {
            if (!_warnedInputException)
            {
                Debug.LogError($"[NRUIRaycastClicker] NRInput hand exception: {e.GetType().Name} {e.Message}");
                _warnedInputException = true;
            }
            return null;
        }
    }

    private void TryControllerRaycast()
    {
        if (TryAnchorRaycast(ControllerAnchorEnum.RightLaserAnchor)) return;
        if (TryAnchorRaycast(ControllerAnchorEnum.LeftLaserAnchor)) return;

        // Fallback to camera forward if anchors are missing
        Camera cam = Camera.main;
        if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit camHit, maxDistance, hitLayers))
        {
            if (HandleHit(camHit)) return;
        }

        TryGraphicRaycast();
    }

    private void TryHandRaycast(HandState handState)
    {
        if (!handState.pointerPoseValid) return;

        Vector3 position = handState.pointerPose.position;
        Vector3 direction = handState.pointerPose.forward;
        if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, hitLayers))
        {
            if (HandleHit(hit)) return;
        }

        TryGraphicRaycast();
    }

    private bool TryAnchorRaycast(ControllerAnchorEnum anchorEnum)
    {
        Transform anchor = NRInput.AnchorsHelper.GetAnchor(anchorEnum);
        if (anchor == null) return false;

        if (Physics.Raycast(anchor.position, anchor.forward, out RaycastHit hit, maxDistance, hitLayers))
        {
            return HandleHit(hit);
        }
        return false;
    }

    private bool HandleHit(RaycastHit hit)
    {
        GameObject target = hit.collider.gameObject;
        if (logHits) Debug.Log($"[NRUIRaycastClicker] Hit: {target.name}");

        return HandleTarget(target);
    }

    private void TryGraphicRaycast()
    {
        if (EventSystem.current == null) return;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = NRInputModule.ScreenCenterPoint
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        if (results.Count == 0)
        {
            if (logHits) Debug.Log("[NRUIRaycastClicker] GraphicRaycast no hits");
            return;
        }

        GameObject target = results[0].gameObject;
        if (logHits) Debug.Log($"[NRUIRaycastClicker] GraphicRaycast hit: {target.name}");
        HandleTarget(target);
    }

    private bool HandleTarget(GameObject target)
    {
        if (logHits)
        {
            Debug.Log($"[NRUIRaycastClicker] Target hierarchy: {GetHierarchyPath(target)}");
            Debug.Log($"[NRUIRaycastClicker] Button in parents: {target.GetComponentInParent<Button>() != null}");
        }
        // Prefer Button if available
        Button button = target.GetComponentInParent<Button>();
        if (button != null && button.interactable)
        {
            if (IsSearchButton(button.gameObject) && button.onClick.GetPersistentEventCount() == 0)
            {
                SearchUIManager searchMgr = FindObjectOfType<SearchUIManager>();
                if (searchMgr != null)
                {
                    searchMgr.ToggleCategoryPanel();
                    return true;
                }
            }
            button.onClick.Invoke();
            return true;
        }

        // Registration list button fallback
        RegistrationListButton regButton = target.GetComponentInParent<RegistrationListButton>();
        if (regButton != null)
        {
            regButton.OnButtonClick();
            return true;
        }

        // Generic pointer click
        if (EventSystem.current != null)
        {
            var eventData = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
            return true;
        }

        return false;
    }

    private string GetHierarchyPath(GameObject target)
    {
        if (target == null) return "(null)";
        Transform t = target.transform;
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }
        return path;
    }

    private bool IsSearchButton(GameObject target)
    {
        if (target == null) return false;
        if (target.name == "SEARCHButton") return true;

        Transform parent = target.transform;
        while (parent != null)
        {
            if (parent.name == "SEARCHButton") return true;
            parent = parent.parent;
        }
        return false;
    }
}
