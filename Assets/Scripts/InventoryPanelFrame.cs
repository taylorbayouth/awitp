using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates a dynamic UI panel frame with 8-slice borders and a colored background.
/// This wraps the inventory UI with decorative border slices.
/// </summary>
[ExecuteAlways]
public class InventoryPanelFrame : MonoBehaviour
{
    [Header("Border Sprites")]
    public Sprite topLeftSprite;
    public Sprite topSprite;
    public Sprite topRightSprite;
    public Sprite rightSprite;
    public Sprite bottomRightSprite;
    public Sprite bottomSprite;
    public Sprite bottomLeftSprite;
    public Sprite leftSprite;

    [Header("Appearance")]
    public Color backgroundColor = new Color(0.38f, 0.36f, 0.34f, 1f); // #615C57
    public float borderThickness = 16f;
    public float padding = 8f;

    [Header("Auto-Sizing")]
    public bool autoSizeToContent = true;

    private RectTransform _containerRect;
    private Image _backgroundImage;
    private Image _topLeftImage;
    private Image _topImage;
    private Image _topRightImage;
    private Image _rightImage;
    private Image _bottomRightImage;
    private Image _bottomImage;
    private Image _bottomLeftImage;
    private Image _leftImage;
    private RectTransform _contentArea;
    private bool _isBuilt = false;

    private static GameObject FindOrCreateChild(Transform parent, string name, params Type[] requiredComponents)
    {
        Transform existing = parent.Find(name);
        GameObject obj;
        if (existing != null)
        {
            obj = existing.gameObject;
            for (int i = 0; i < requiredComponents.Length; i++)
            {
                Type componentType = requiredComponents[i];
                if (componentType == null) continue;
                if (obj.GetComponent(componentType) == null)
                {
                    obj.AddComponent(componentType);
                }
            }
            return obj;
        }

        obj = new GameObject(name, requiredComponents);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static Transform ChoosePreferred(Transform[] candidates)
    {
        if (candidates == null || candidates.Length == 0) return null;

        Transform preferred = candidates[0];
        int preferredChildren = preferred != null ? preferred.childCount : -1;
        for (int i = 1; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null) continue;
            int childCount = candidate.childCount;
            if (childCount > preferredChildren)
            {
                preferred = candidate;
                preferredChildren = childCount;
            }
        }
        return preferred;
    }

    private void CleanupDuplicateNamedChildren()
    {
        if (Application.isPlaying) return;

        string[] names =
        {
            "Background",
            "ContentArea",
            "TopLeft",
            "Top",
            "TopRight",
            "Right",
            "BottomRight",
            "Bottom",
            "BottomLeft",
            "Left"
        };

        for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
        {
            string target = names[nameIndex];
            List<Transform> matches = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name == target)
                {
                    matches ??= new List<Transform>();
                    matches.Add(child);
                }
            }

            if (matches == null || matches.Count <= 1) continue;

            Transform keeper = ChoosePreferred(matches.ToArray());
            for (int i = 0; i < matches.Count; i++)
            {
                Transform candidate = matches[i];
                if (candidate == null || candidate == keeper) continue;
                DestroyImmediate(candidate.gameObject);
            }
        }
    }

    private void Awake()
    {
        BuildFrame();
    }

    private void OnEnable()
    {
        if (!_isBuilt)
        {
            BuildFrame();
        }
    }

    private void Update()
    {
        if (!_isBuilt)
        {
            BuildFrame();
        }
        UpdateFrame();
        UpdateFrameSize();
    }

    private void BuildFrame()
    {
        _containerRect = GetComponent<RectTransform>();
        if (_containerRect == null)
        {
            Debug.LogWarning("[InventoryPanelFrame] No RectTransform found on this GameObject");
            return;
        }

        CleanupDuplicateNamedChildren();

        // Create/bind background
        GameObject bgObj = FindOrCreateChild(transform, "Background", typeof(RectTransform), typeof(Image));
        _backgroundImage = bgObj.GetComponent<Image>();
        _backgroundImage.color = backgroundColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(borderThickness, borderThickness);
        bgRect.offsetMax = new Vector2(-borderThickness, -borderThickness);

        // Create/bind content area (this is where the inventory panel goes)
        GameObject contentObj = FindOrCreateChild(transform, "ContentArea", typeof(RectTransform), typeof(LayoutElement));
        _contentArea = contentObj.GetComponent<RectTransform>();
        _contentArea.anchorMin = Vector2.zero;
        _contentArea.anchorMax = Vector2.one;
        float totalInset = borderThickness + padding;
        _contentArea.offsetMin = new Vector2(totalInset, totalInset);
        _contentArea.offsetMax = new Vector2(-totalInset, -totalInset);

        // Set up layout element to account for borders and padding
        LayoutElement contentLayout = contentObj.GetComponent<LayoutElement>();
        contentLayout.ignoreLayout = false;

        // Create border pieces
        _topLeftImage = CreateBorderImage("TopLeft", topLeftSprite);
        _topImage = CreateBorderImage("Top", topSprite);
        _topRightImage = CreateBorderImage("TopRight", topRightSprite);
        _rightImage = CreateBorderImage("Right", rightSprite);
        _bottomRightImage = CreateBorderImage("BottomRight", bottomRightSprite);
        _bottomImage = CreateBorderImage("Bottom", bottomSprite);
        _bottomLeftImage = CreateBorderImage("BottomLeft", bottomLeftSprite);
        _leftImage = CreateBorderImage("Left", leftSprite);

        EnsureBorderSpritesLoaded();
        ApplyBorderSprites();
        _isBuilt = true;
        UpdateBorderPositions();
    }

    private Image CreateBorderImage(string name, Sprite sprite)
    {
        GameObject obj = FindOrCreateChild(transform, name, typeof(RectTransform), typeof(Image));
        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.enabled = sprite != null;
        return img;
    }

    private void UpdateFrame()
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = backgroundColor;
        }
        ApplyBorderSprites();
        UpdateBorderPositions();
    }

    private void ApplyBorderSprites()
    {
        ApplyBorderSprite(_topLeftImage, topLeftSprite);
        ApplyBorderSprite(_topImage, topSprite);
        ApplyBorderSprite(_topRightImage, topRightSprite);
        ApplyBorderSprite(_rightImage, rightSprite);
        ApplyBorderSprite(_bottomRightImage, bottomRightSprite);
        ApplyBorderSprite(_bottomImage, bottomSprite);
        ApplyBorderSprite(_bottomLeftImage, bottomLeftSprite);
        ApplyBorderSprite(_leftImage, leftSprite);
    }

    private static void ApplyBorderSprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
        if (sprite == null)
        {
            image.color = Color.clear;
        }
        else
        {
            image.color = Color.white;
        }
    }

    private void UpdateBorderPositions()
    {
        if (_containerRect == null) return;

        float width = _containerRect.rect.width;
        float height = _containerRect.rect.height;

        // Top-left corner
        if (_topLeftImage != null)
        {
            RectTransform rect = _topLeftImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(borderThickness, borderThickness);
        }

        // Top edge
        if (_topImage != null)
        {
            RectTransform rect = _topImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(borderThickness, -borderThickness);
            rect.offsetMax = new Vector2(-borderThickness, 0f);
        }

        // Top-right corner
        if (_topRightImage != null)
        {
            RectTransform rect = _topRightImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(borderThickness, borderThickness);
        }

        // Right edge
        if (_rightImage != null)
        {
            RectTransform rect = _rightImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(-borderThickness, borderThickness);
            rect.offsetMax = new Vector2(0f, -borderThickness);
        }

        // Bottom-right corner
        if (_bottomRightImage != null)
        {
            RectTransform rect = _bottomRightImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(borderThickness, borderThickness);
        }

        // Bottom edge
        if (_bottomImage != null)
        {
            RectTransform rect = _bottomImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(borderThickness, 0f);
            rect.offsetMax = new Vector2(-borderThickness, borderThickness);
        }

        // Bottom-left corner
        if (_bottomLeftImage != null)
        {
            RectTransform rect = _bottomLeftImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(borderThickness, borderThickness);
        }

        // Left edge
        if (_leftImage != null)
        {
            RectTransform rect = _leftImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(0f, borderThickness);
            rect.offsetMax = new Vector2(borderThickness, -borderThickness);
        }
    }

    private void UpdateFrameSize()
    {
        if (!autoSizeToContent || _contentArea == null || _containerRect == null)
        {
            return;
        }

        // Calculate the required size based on content
        float contentWidth = 0f;
        float contentHeight = 0f;

        // Check all children in the content area
        foreach (RectTransform child in _contentArea)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            // Get the layout element or rect size
            LayoutElement layoutElement = child.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                contentWidth = Mathf.Max(contentWidth, layoutElement.preferredWidth);
                contentHeight = Mathf.Max(contentHeight, layoutElement.preferredHeight);
            }
            else
            {
                contentWidth = Mathf.Max(contentWidth, child.rect.width);
                contentHeight = Mathf.Max(contentHeight, child.rect.height);
            }
        }

        // Add borders and padding
        float totalInset = (borderThickness + padding) * 2f;
        float frameWidth = contentWidth + totalInset;
        float frameHeight = contentHeight + totalInset;

        // Update frame size
        if (frameWidth > 0 && frameHeight > 0)
        {
            _containerRect.sizeDelta = new Vector2(frameWidth, frameHeight);
        }
    }

    /// <summary>
    /// Gets the content area where inventory elements should be placed
    /// </summary>
    public RectTransform GetContentArea()
    {
        if (!_isBuilt)
        {
            BuildFrame();
        }
        return _contentArea;
    }

    /// <summary>
    /// Loads border sprites from the specified path
    /// </summary>
    public void LoadBorderSprites(string spritePath = "Assets/Sprites/inventoryUi")
    {
        topLeftSprite = LoadSprite($"{spritePath}/ui-ul");
        topSprite = LoadSprite($"{spritePath}/ui-t");
        topRightSprite = LoadSprite($"{spritePath}/ui-ur");
        rightSprite = LoadSprite($"{spritePath}/ui-r");
        bottomRightSprite = LoadSprite($"{spritePath}/ui-br");
        bottomSprite = LoadSprite($"{spritePath}/ui-b");
        bottomLeftSprite = LoadSprite($"{spritePath}/ui-bl");
        leftSprite = LoadSprite($"{spritePath}/ui-l");
    }

    private Sprite LoadSprite(string path)
    {
        // Try loading from Resources first
        string resourcePath = path.Replace("Assets/Resources/", "").Replace(".png", "");
        Sprite sprite = Resources.Load<Sprite>(resourcePath);

#if UNITY_EDITOR
        if (sprite == null)
        {
            string assetPath = path.EndsWith(".png") ? path : $"{path}.png";
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    string foundPath = AssetDatabase.GetAssetPath(texture);
                    UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(foundPath);
                    foreach (UnityEngine.Object asset in assets)
                    {
                        if (asset is Sprite s)
                        {
                            sprite = s;
                            break;
                        }
                    }
                }
            }
        }
#endif

        if (sprite == null)
        {
            Debug.LogWarning($"[InventoryPanelFrame] Could not load sprite at: {path}");
        }

        return sprite;
    }

    private void EnsureBorderSpritesLoaded()
    {
        if (topLeftSprite == null ||
            topSprite == null ||
            topRightSprite == null ||
            rightSprite == null ||
            bottomRightSprite == null ||
            bottomSprite == null ||
            bottomLeftSprite == null ||
            leftSprite == null)
        {
            LoadBorderSprites();
        }
    }
}
