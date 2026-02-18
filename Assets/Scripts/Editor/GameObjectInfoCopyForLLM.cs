using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies comprehensive GameObject information to the clipboard in a format
/// optimized for sharing with LLMs to diagnose issues.
/// Accessible from any component's context menu or the GameObject menu.
/// </summary>
public static class GameObjectInfoCopyForLLM
{
    [MenuItem("CONTEXT/Component/Copy Full GameObject Info for LLM")]
    private static void CopyFromComponentContext(MenuCommand command)
    {
        if (command.context is Component component)
            CopyGameObjectInfo(component.gameObject);
    }

    [MenuItem("GameObject/Copy Full GameObject Info for LLM", false, 49)]
    private static void CopyFromGameObjectMenu()
    {
        if (Selection.activeGameObject != null)
            CopyGameObjectInfo(Selection.activeGameObject);
    }

    [MenuItem("GameObject/Copy Full GameObject Info for LLM", true)]
    private static bool ValidateCopyFromGameObjectMenu()
    {
        return Selection.activeGameObject != null;
    }

    private static void CopyGameObjectInfo(GameObject go)
    {
        var sb = new StringBuilder(8192);

        AppendGameObjectBasics(sb, go);
        AppendPrefabInfo(sb, go);
        AppendTransformInfo(sb, go.transform);
        AppendParentChain(sb, go.transform);
        AppendChildHierarchy(sb, go.transform);
        AppendAllComponents(sb, go);

        GUIUtility.systemCopyBuffer = sb.ToString().TrimEnd();
        Debug.Log($"[LLM Copy] Copied full GameObject info for \"{go.name}\" to clipboard.");
    }

    // ----------------------------------------------------------------
    // Sections
    // ----------------------------------------------------------------

    private static void AppendGameObjectBasics(StringBuilder sb, GameObject go)
    {
        sb.AppendLine("=== GAMEOBJECT ===");
        sb.AppendLine($"Name: {go.name}");
        sb.AppendLine($"Active Self: {go.activeSelf}");
        sb.AppendLine($"Active In Hierarchy: {go.activeInHierarchy}");
        sb.AppendLine($"Tag: {go.tag}");
        sb.AppendLine($"Layer: {go.layer} ({LayerMask.LayerToName(go.layer)})");
        sb.AppendLine($"Static: {go.isStatic}");

        if (go.isStatic)
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            sb.AppendLine($"Static Flags: {flags}");
        }

        sb.AppendLine($"Scene: {go.scene.name ?? "(none)"}");
        sb.AppendLine();
    }

    private static void AppendPrefabInfo(StringBuilder sb, GameObject go)
    {
        var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(go);
        var assetType = PrefabUtility.GetPrefabAssetType(go);

        // Not a prefab at all
        if (instanceStatus == PrefabInstanceStatus.NotAPrefab &&
            assetType == PrefabAssetType.NotAPrefab)
        {
            return;
        }

        sb.AppendLine("=== PREFAB INFO ===");

        if (instanceStatus != PrefabInstanceStatus.NotAPrefab)
        {
            // Scene instance of a prefab
            sb.AppendLine($"Instance Status: {instanceStatus}");
            sb.AppendLine($"Asset Type: {assetType}");

            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (source != null)
                sb.AppendLine($"Source Prefab: {AssetDatabase.GetAssetPath(source)}");

            var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (nearest != null && nearest != go)
                sb.AppendLine($"Nearest Prefab Root: {nearest.name}");

            var overrides = PrefabUtility.GetObjectOverrides(go, false);
            var addedComponents = PrefabUtility.GetAddedComponents(go);
            var removedComponents = PrefabUtility.GetRemovedComponents(go);
            var addedObjects = PrefabUtility.GetAddedGameObjects(go);

            sb.AppendLine($"Property Overrides: {overrides.Count}");
            sb.AppendLine($"Added Components: {addedComponents.Count}");
            sb.AppendLine($"Removed Components: {removedComponents.Count}");
            sb.AppendLine($"Added Child GameObjects: {addedObjects.Count}");
        }
        else
        {
            // Prefab asset being inspected directly
            sb.AppendLine($"Is Prefab Asset: true");
            sb.AppendLine($"Asset Type: {assetType}");
            sb.AppendLine($"Asset Path: {AssetDatabase.GetAssetPath(go)}");
        }

        sb.AppendLine();
    }

    private static void AppendTransformInfo(StringBuilder sb, Transform t)
    {
        sb.AppendLine("=== TRANSFORM ===");
        sb.AppendLine($"Local Position: {FormatVector3(t.localPosition)}");
        sb.AppendLine($"Local Rotation (Euler): {FormatVector3(t.localEulerAngles)}");
        sb.AppendLine($"Local Scale: {FormatVector3(t.localScale)}");
        sb.AppendLine($"World Position: {FormatVector3(t.position)}");
        sb.AppendLine($"World Rotation (Euler): {FormatVector3(t.eulerAngles)}");
        sb.AppendLine($"Lossy Scale: {FormatVector3(t.lossyScale)}");
        sb.AppendLine();
    }

    private static void AppendParentChain(StringBuilder sb, Transform t)
    {
        if (t.parent == null) return;

        sb.AppendLine("=== PARENT CHAIN (immediate → root) ===");
        var current = t.parent;
        int depth = 0;
        while (current != null)
        {
            var components = current.GetComponents<Component>();
            var compList = BuildComponentTypeList(components);
            string indent = new string(' ', depth * 2);
            string compStr = compList.Length > 0 ? $" [{compList}]" : "";
            string active = current.gameObject.activeSelf ? "" : " (INACTIVE)";
            sb.AppendLine($"{indent}{current.name}{compStr}{active}");
            current = current.parent;
            depth++;
        }
        sb.AppendLine();
    }

    private static void AppendChildHierarchy(StringBuilder sb, Transform root)
    {
        if (root.childCount == 0) return;

        sb.AppendLine($"=== CHILDREN HIERARCHY ({CountDescendants(root)} descendants) ===");
        AppendChildTree(sb, root, 0);
        sb.AppendLine();
    }

    private static void AppendChildTree(StringBuilder sb, Transform parent, int depth)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            bool isLast = i == parent.childCount - 1;

            // Build tree-drawing prefix
            string prefix = new string(' ', depth * 3) + (isLast ? "└─ " : "├─ ");

            var components = child.GetComponents<Component>();
            var compList = BuildComponentTypeList(components);
            string compStr = compList.Length > 0 ? $" ({compList})" : "";
            string active = child.gameObject.activeSelf ? "" : " [INACTIVE]";

            sb.AppendLine($"{prefix}{child.name}{compStr}{active}");

            if (child.childCount > 0)
                AppendChildTree(sb, child, depth + 1);
        }
    }

    private static void AppendAllComponents(StringBuilder sb, GameObject go)
    {
        sb.AppendLine("=== COMPONENTS ===");
        var components = go.GetComponents<Component>();

        foreach (var component in components)
        {
            if (component == null)
            {
                sb.AppendLine("--- [Missing Script] ---");
                sb.AppendLine();
                continue;
            }

            // Transform is already shown in its own section
            if (component is Transform) continue;

            sb.AppendLine($"--- {component.GetType().Name} ---");

            // Enabled state
            AppendEnabledState(sb, component);

            // Script file path for MonoBehaviours
            if (component is MonoBehaviour mb)
            {
                var script = MonoScript.FromMonoBehaviour(mb);
                if (script != null)
                    sb.AppendLine($"Script: {AssetDatabase.GetAssetPath(script)}");
            }

            // All serialized properties
            var serialized = new SerializedObject(component);
            var iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script") continue;

                string label = iterator.displayName;
                string value = ComponentSettingsCopyForLLM.GetPropertyValueString(iterator);
                sb.AppendLine($"{label}: {value}");
            }

            // Renderer extras: materials, shader names, bounds
            if (component is Renderer renderer)
                AppendRendererExtras(sb, renderer);

            // Rigidbody extras
            if (component is Rigidbody rb)
                AppendRigidbodyExtras(sb, rb);

            // Collider extras
            if (component is Collider col)
                AppendColliderExtras(sb, col);

            sb.AppendLine();
        }
    }

    // ----------------------------------------------------------------
    // Extras for specific component types
    // ----------------------------------------------------------------

    private static void AppendEnabledState(StringBuilder sb, Component component)
    {
        if (component is Behaviour behaviour)
            sb.AppendLine($"Enabled: {behaviour.enabled}");
        else if (component is Renderer renderer)
            sb.AppendLine($"Enabled: {renderer.enabled}");
        else if (component is Collider collider)
            sb.AppendLine($"Enabled: {collider.enabled}");
    }

    private static void AppendRendererExtras(StringBuilder sb, Renderer renderer)
    {
        sb.AppendLine($"Bounds Center: {FormatVector3(renderer.bounds.center)}");
        sb.AppendLine($"Bounds Size: {FormatVector3(renderer.bounds.size)}");

        var materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            if (mat != null)
            {
                sb.AppendLine($"Material[{i}]: {mat.name} (Shader: {mat.shader.name})");
                string matPath = AssetDatabase.GetAssetPath(mat);
                if (!string.IsNullOrEmpty(matPath))
                    sb.AppendLine($"  Material Path: {matPath}");
            }
            else
            {
                sb.AppendLine($"Material[{i}]: null");
            }
        }
    }

    private static void AppendRigidbodyExtras(StringBuilder sb, Rigidbody rb)
    {
        sb.AppendLine($"Velocity: {FormatVector3(rb.linearVelocity)}");
        sb.AppendLine($"Angular Velocity: {FormatVector3(rb.angularVelocity)}");
        sb.AppendLine($"Is Sleeping: {rb.IsSleeping()}");
    }

    private static void AppendColliderExtras(StringBuilder sb, Collider col)
    {
        sb.AppendLine($"Collider Bounds Center: {FormatVector3(col.bounds.center)}");
        sb.AppendLine($"Collider Bounds Size: {FormatVector3(col.bounds.size)}");
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static string FormatVector3(Vector3 v)
    {
        return $"({v.x:G5}, {v.y:G5}, {v.z:G5})";
    }

    private static string BuildComponentTypeList(Component[] components)
    {
        var sb = new StringBuilder();
        foreach (var c in components)
        {
            if (c == null)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("[Missing]");
                continue;
            }
            if (c is Transform) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(c.GetType().Name);
        }
        return sb.ToString();
    }

    private static int CountDescendants(Transform t)
    {
        int count = t.childCount;
        for (int i = 0; i < t.childCount; i++)
            count += CountDescendants(t.GetChild(i));
        return count;
    }
}
