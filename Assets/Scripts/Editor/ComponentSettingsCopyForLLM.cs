using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies a single component's serialized properties to the clipboard
/// in a format optimized for sharing with LLMs.
/// </summary>
public static class ComponentSettingsCopyForLLM
{
    private const string MenuPath = "CONTEXT/Component/Copy component settings for LLM";

    [MenuItem(MenuPath)]
    private static void CopyComponentSettings(MenuCommand command)
    {
        if (command.context is not Component component)
        {
            return;
        }

        StringBuilder builder = new StringBuilder(1024);
        builder.AppendLine($"Component: {component.GetType().Name}");
        builder.AppendLine($"GameObject: {component.gameObject.name}");

        // Enabled state
        if (component is Behaviour behaviour)
            builder.AppendLine($"Enabled: {behaviour.enabled}");
        else if (component is Renderer renderer)
            builder.AppendLine($"Enabled: {renderer.enabled}");
        else if (component is Collider collider)
            builder.AppendLine($"Enabled: {collider.enabled}");

        // Script file path for MonoBehaviours
        if (component is MonoBehaviour mb)
        {
            var script = MonoScript.FromMonoBehaviour(mb);
            if (script != null)
                builder.AppendLine($"Script: {AssetDatabase.GetAssetPath(script)}");
        }

        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty iterator = serialized.GetIterator();

        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.propertyPath == "m_Script")
            {
                continue;
            }

            string label = iterator.displayName;
            string value = GetPropertyValueString(iterator);
            builder.AppendLine($"{label}: {value}");
        }

        GUIUtility.systemCopyBuffer = builder.ToString().TrimEnd();
        Debug.Log($"[LLM Copy] Copied {component.GetType().Name} settings to clipboard.");
    }

    /// <summary>
    /// Converts a SerializedProperty to a human-readable string.
    /// Public so GameObjectInfoCopyForLLM can reuse it.
    /// </summary>
    public static string GetPropertyValueString(SerializedProperty property)
    {
        return GetPropertyValueString(property, 0);
    }

    private static string GetPropertyValueString(SerializedProperty property, int depth)
    {
        // Guard against excessive recursion
        if (depth > 4)
            return $"({property.type})";

        if (property.isArray && property.propertyType != SerializedPropertyType.String)
        {
            int size = property.arraySize;
            if (size == 0)
            {
                return "Array(size=0)";
            }

            StringBuilder arrayBuilder = new StringBuilder();
            arrayBuilder.Append($"Array(size={size}) [");
            for (int i = 0; i < size; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                if (i > 0)
                {
                    arrayBuilder.Append(", ");
                }
                arrayBuilder.Append(GetPropertyValueString(element, depth + 1));
            }
            arrayBuilder.Append(']');
            return arrayBuilder.ToString();
        }

        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return property.boolValue ? "true" : "false";
            case SerializedPropertyType.Float:
                return property.floatValue.ToString("G");
            case SerializedPropertyType.String:
                return string.IsNullOrEmpty(property.stringValue) ? "(empty)" : property.stringValue;
            case SerializedPropertyType.Color:
                return property.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return FormatObjectReference(property.objectReferenceValue);
            case SerializedPropertyType.LayerMask:
                return FormatLayerMask(property.intValue);
            case SerializedPropertyType.Enum:
                return FormatEnum(property);
            case SerializedPropertyType.Vector2:
                return property.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return FormatVector3(property.vector3Value);
            case SerializedPropertyType.Vector4:
                return property.vector4Value.ToString();
            case SerializedPropertyType.Rect:
                return property.rectValue.ToString();
            case SerializedPropertyType.Bounds:
                return property.boundsValue.ToString();
            case SerializedPropertyType.Quaternion:
                return property.quaternionValue.ToString();
            case SerializedPropertyType.Vector2Int:
                return property.vector2IntValue.ToString();
            case SerializedPropertyType.Vector3Int:
                return property.vector3IntValue.ToString();
            case SerializedPropertyType.RectInt:
                return property.rectIntValue.ToString();
            case SerializedPropertyType.BoundsInt:
                return property.boundsIntValue.ToString();
            case SerializedPropertyType.AnimationCurve:
                return FormatCurve(property.animationCurveValue);
            case SerializedPropertyType.ExposedReference:
                return FormatObjectReference(property.exposedReferenceValue);
            case SerializedPropertyType.Gradient:
                return "Gradient(...)";
            case SerializedPropertyType.Generic:
                return FormatGeneric(property, depth);
            default:
                return property.type;
        }
    }

    private static string FormatObjectReference(Object obj)
    {
        if (obj == null)
        {
            return "null";
        }

        string path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path))
            return $"{obj.name} ({obj.GetType().Name}, {path})";

        return $"{obj.name} ({obj.GetType().Name})";
    }

    private static string FormatEnum(SerializedProperty property)
    {
        int index = property.enumValueIndex;
        if (index >= 0 && index < property.enumDisplayNames.Length)
        {
            return property.enumDisplayNames[index];
        }
        return index.ToString();
    }

    private static string FormatCurve(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return "AnimationCurve(empty)";
        }

        var sb = new StringBuilder();
        sb.Append($"AnimationCurve(keys={curve.length}) [");
        for (int i = 0; i < curve.length; i++)
        {
            var key = curve[i];
            if (i > 0) sb.Append(", ");
            sb.Append($"({key.time:G3}, {key.value:G3})");
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static string FormatVector3(Vector3 v)
    {
        return $"({v.x:G5}, {v.y:G5}, {v.z:G5})";
    }

    private static string FormatLayerMask(int mask)
    {
        if (mask == 0) return "Nothing";
        if (mask == ~0) return "Everything";

        var sb = new StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName)) continue;
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append(layerName);
            }
        }
        return sb.Length > 0 ? sb.ToString() : mask.ToString();
    }

    private static string FormatGeneric(SerializedProperty property, int depth)
    {
        if (!property.hasVisibleChildren)
            return property.type;

        // Recurse into child properties for inline display
        var sb = new StringBuilder();
        sb.Append("{ ");

        var child = property.Copy();
        var endProperty = property.GetEndProperty();
        bool first = true;

        if (child.NextVisible(true))
        {
            while (!SerializedProperty.EqualContents(child, endProperty))
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append($"{child.displayName}: {GetPropertyValueString(child, depth + 1)}");
                if (!child.NextVisible(false)) break;
            }
        }

        sb.Append(" }");
        return sb.ToString();
    }
}
