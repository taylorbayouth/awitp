using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zoom toggle button (+ / -) in the upper-right corner, visible only during Play mode.
/// Created programmatically by CameraSetup — no manual scene setup required.
/// Assign the Koulen-Regular font on CameraSetup's "Zoom Button Font" field.
/// </summary>
public class ZoomToggleUI : MonoBehaviour
{
    private CameraSetup cameraSetup;
    private BuilderController builderController;
    private GameObject canvasObj;
    private Text label;

    public void Initialize(CameraSetup setup, Font font)
    {
        cameraSetup = setup;
        // Don't fetch BuilderController yet — it may not be registered at Start() time.
        // We'll fetch it lazily in Update().

        Font resolvedFont = font != null
            ? font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildUI(resolvedFont);

        Debug.Log($"[ZoomToggleUI] Initialize complete. Font: {font?.name}");
    }

    private void BuildUI(Font font)
    {
        // Screen-space overlay canvas (renders on top of everything)
        canvasObj = new GameObject("ZoomCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Button — anchored upper-right with comfortable tap target
        GameObject btnObj = new GameObject("ZoomButton");
        btnObj.transform.SetParent(canvasObj.transform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-24f, -24f);
        btnRect.sizeDelta = new Vector2(80f, 80f);

        // Transparent background (Button requires an Image for raycasting)
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        Button button = btnObj.AddComponent<Button>();
        button.onClick.AddListener(OnClick);
        button.transition = Selectable.Transition.None;

        // Label text
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObj.AddComponent<Text>();
        label.text = "+";
        label.font = font;
        label.fontSize = 56;
        label.color = Color.black;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.resizeTextForBestFit = false;
        label.raycastTarget = false; // Only the button needs raycasts

        // Unity Text requires a material — use the font's default material
        if (font != null && font.material != null)
        {
            label.material = font.material;
        }
        else
        {
            // Fallback: create a basic unlit material for the font texture
            Shader shader = Shader.Find("UI/Default");
            if (shader != null && font != null)
            {
                Material mat = new Material(shader);
                mat.mainTexture = font.material != null ? font.material.mainTexture : null;
                label.material = mat;
            }
        }

        Debug.Log($"[ZoomToggleUI] Label created. Font: {font?.name}, Material: {label.material?.name}, Color: {label.color}");

        // Start hidden until Play mode
        canvasObj.SetActive(false);
    }

    private void Update()
    {
        if (canvasObj == null || cameraSetup == null) return;

        // Lazy lookup of BuilderController (may not be registered at Start() time)
        if (builderController == null)
        {
            builderController = ServiceRegistry.Get<BuilderController>(logIfMissing: false);
        }

        bool playMode = builderController != null &&
                        builderController.currentMode == GameMode.Play;

        if (canvasObj.activeSelf != playMode)
        {
            canvasObj.SetActive(playMode);
            if (playMode)
            {
                Debug.Log($"[ZoomToggleUI] Canvas activated. Label text: '{label?.text}', Material: {label?.material?.name}");
            }
        }

        if (playMode && label != null)
        {
            label.text = cameraSetup.IsZoomed ? "-" : "+";
        }
    }

    private void OnClick()
    {
        if (cameraSetup != null)
        {
            cameraSetup.ToggleZoom();
        }
    }
}
