using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Small helper to unify mouse/touch pointer detection.
/// Extend here later for multi-touch, drag, swipe, etc.
/// </summary>
public static class PointerInput
{
    public const int MousePointerId = -1;
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>(8);

    private static bool enhancedTouchEnabled;

    private static void EnsureEnhancedTouchEnabled()
    {
        if (!enhancedTouchEnabled)
        {
            EnhancedTouchSupport.Enable();
            enhancedTouchEnabled = true;
        }
    }

    public static bool TryGetPrimaryPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        EnsureEnhancedTouchEnabled();

        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                screenPosition = touch.screenPosition;
                pointerId = touch.touchId;
                return true;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pointerId = MousePointerId;
            return true;
        }

        screenPosition = Vector2.zero;
        pointerId = MousePointerId;
        return false;
    }

    public static bool IsPointerOverUI(Vector2 screenPosition, int pointerId)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition,
            pointerId = pointerId
        };

        RaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastResults);
        return RaycastResults.Count > 0;
    }
}
