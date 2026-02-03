using System.Text;
using UnityEditor;
using UnityEngine;

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
        Debug.Log($"Copied {component.GetType().Name} settings to clipboard for LLM.");
    }

    private static string GetPropertyValueString(SerializedProperty property)
    {
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
                arrayBuilder.Append(GetPropertyValueString(element));
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
                return property.stringValue ?? string.Empty;
            case SerializedPropertyType.Color:
                return property.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return FormatObjectReference(property.objectReferenceValue);
            case SerializedPropertyType.LayerMask:
                return property.intValue.ToString();
            case SerializedPropertyType.Enum:
                return FormatEnum(property);
            case SerializedPropertyType.Vector2:
                return property.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return property.vector3Value.ToString();
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
            case SerializedPropertyType.Generic:
                return property.hasVisibleChildren ? "{...}" : property.type;
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
        if (curve == null)
        {
            return "AnimationCurve(null)";
        }
        return $"AnimationCurve(keys={curve.length})";
    }
}
