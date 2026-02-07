using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Small helper to unify mouse/touch pointer detection.
/// Extend here later for multi-touch, drag, swipe, etc.
/// </summary>
public static class PointerInput
{
    public const int MousePointerId = -1;

    public static bool TryGetPrimaryPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
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

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}
