#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Editor utilities for setting up the Level System.
/// Provides menu items to create scenes, assets, and configure the game.
///
/// ACCESS: Window > AWITP > Level System Setup
/// </summary>
public class LevelSystemSetup : EditorWindow
{
    [MenuItem("Window/AWITP/Level System Setup")]
    public static void ShowWindow()
    {
        GetWindow<LevelSystemSetup>("Level System Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("A Walk in the Park - Level System Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("Scene Generation", EditorStyles.boldLabel);
        if (GUILayout.Button("Create MainMenu Scene"))
        {
            CreateMainMenuScene();
        }

        if (GUILayout.Button("Create WorldMap Scene"))
        {
            CreateWorldMapScene();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Asset Creation", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Sample World (Tutorial)"))
        {
            CreateSampleTutorialWorld();
        }

        if (GUILayout.Button("Create Level Definition Template"))
        {
            CreateLevelDefinitionTemplate();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Scene Configuration", EditorStyles.boldLabel);
        if (GUILayout.Button("Add VictoryScreen to Current Scene"))
        {
            AddVictoryScreenToScene();
        }

        if (GUILayout.Button("Add Level System Managers to Current Scene"))
        {
            AddLevelSystemManagers();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Build Settings", EditorStyles.boldLabel);
        if (GUILayout.Button("Add All AWITP Scenes to Build Settings"))
        {
            AddScenesToBuildSettings();
        }
    }

    #region Scene Creation

    [MenuItem("Window/AWITP/Create MainMenu Scene")]
    public static void CreateMainMenuScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create Main Panel
        GameObject mainPanel = CreateUIPanel(canvasObj.transform, "MainPanel");

        // Create Title
        CreateUIText(mainPanel.transform, "TitleText", "A Walk in the Park", 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0, 0), new Vector2(600, 80));

        // Create Buttons
        GameObject playBtn = CreateUIButton(mainPanel.transform, "PlayButton", "Play",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(200, 50));

        GameObject continueBtn = CreateUIButton(mainPanel.transform, "ContinueButton", "Continue",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(200, 50));

        GameObject settingsBtn = CreateUIButton(mainPanel.transform, "SettingsButton", "Settings",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(200, 50));

        GameObject quitBtn = CreateUIButton(mainPanel.transform, "QuitButton", "Quit",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(200, 50));

        // Create Version Text
        CreateUIText(mainPanel.transform, "VersionText", "v1.0.0", 14, TextAnchor.LowerRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10, 10), new Vector2(100, 30));

        // Create Settings Panel (hidden)
        GameObject settingsPanel = CreateUIPanel(canvasObj.transform, "SettingsPanel");
        settingsPanel.SetActive(false);

        // Add MainMenuController
        GameObject controllerObj = new GameObject("MainMenuController");
        System.Type controllerType = System.Type.GetType("MainMenuController");
        if (controllerType == null)
        {
            Debug.LogError("Could not find MainMenuController type.");
            return;
        }
        Component controller = controllerObj.AddComponent(controllerType);

        // Wire up references via SerializedObject
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<Button>();
        so.FindProperty("continueButton").objectReferenceValue = continueBtn.GetComponent<Button>();
        so.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
        so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("titleText").objectReferenceValue = mainPanel.transform.Find("TitleText").GetComponent<Text>();
        so.FindProperty("versionText").objectReferenceValue = mainPanel.transform.Find("VersionText").GetComponent<Text>();
        so.ApplyModifiedProperties();

        // Add EventSystem
        if (ServiceRegistry.Get<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Save scene
        string scenePath = "Assets/Scenes/MainMenu.unity";
        EnsureDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log($"MainMenu scene created at: {scenePath}");
    }

    [MenuItem("Window/AWITP/Create WorldMap Scene")]
    public static void CreateWorldMapScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create Main Panel
        GameObject mainPanel = CreateUIPanel(canvasObj.transform, "MainPanel");

        // Create Title
        CreateUIText(mainPanel.transform, "TitleText", "World Map", 36, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), new Vector2(0, 0), new Vector2(400, 60));

        // Create World Button Container with VerticalLayoutGroup
        GameObject worldContainer = new GameObject("WorldButtonContainer");
        worldContainer.transform.SetParent(mainPanel.transform, false);
        RectTransform containerRect = worldContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.2f);
        containerRect.anchorMax = new Vector2(0.9f, 0.85f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = worldContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Create Back Button
        GameObject backBtn = CreateUIButton(mainPanel.transform, "BackButton", "Back",
            new Vector2(0.1f, 0.05f), new Vector2(0.1f, 0.05f), new Vector2(0, 0), new Vector2(100, 40));

        // Create Level Select Panel (hidden initially)
        GameObject levelSelectPanel = CreateUIPanel(canvasObj.transform, "LevelSelectPanel");
        levelSelectPanel.SetActive(false);

        // Add close button to level select
        GameObject closeLevelBtn = CreateUIButton(levelSelectPanel.transform, "CloseButton", "X",
            new Vector2(0.95f, 0.95f), new Vector2(0.95f, 0.95f), new Vector2(0, 0), new Vector2(40, 40));

        // Create World Name Text in level select panel
        CreateUIText(levelSelectPanel.transform, "WorldNameText", "World Name", 28, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0, 0), new Vector2(400, 50));

        // Create Progress Text
        CreateUIText(levelSelectPanel.transform, "WorldProgressText", "0/5 Complete", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0, 0), new Vector2(200, 30));

        // Create Level Button Container
        GameObject levelContainer = new GameObject("LevelButtonContainer");
        levelContainer.transform.SetParent(levelSelectPanel.transform, false);
        RectTransform levelContainerRect = levelContainer.AddComponent<RectTransform>();
        levelContainerRect.anchorMin = new Vector2(0.1f, 0.15f);
        levelContainerRect.anchorMax = new Vector2(0.9f, 0.75f);
        levelContainerRect.offsetMin = Vector2.zero;
        levelContainerRect.offsetMax = Vector2.zero;

        GridLayoutGroup glg = levelContainer.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(120, 80);
        glg.spacing = new Vector2(15, 15);
        glg.childAlignment = TextAnchor.UpperLeft;

        // Add WorldMapController
        GameObject controllerObj = new GameObject("WorldMapController");
        System.Type controllerType = System.Type.GetType("WorldMapController");
        if (controllerType == null)
        {
            Debug.LogError("Could not find WorldMapController type.");
            return;
        }
        Component controller = controllerObj.AddComponent(controllerType);

        // Wire up references
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("worldButtonContainer").objectReferenceValue = worldContainer.transform;
        so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        so.FindProperty("levelSelectPanel").objectReferenceValue = levelSelectPanel;
        so.FindProperty("levelButtonContainer").objectReferenceValue = levelContainer.transform;
        so.FindProperty("closeLevelSelectButton").objectReferenceValue = closeLevelBtn.GetComponent<Button>();
        so.FindProperty("worldNameText").objectReferenceValue = levelSelectPanel.transform.Find("WorldNameText").GetComponent<Text>();
        so.FindProperty("worldProgressText").objectReferenceValue = levelSelectPanel.transform.Find("WorldProgressText").GetComponent<Text>();
        so.ApplyModifiedProperties();

        // Add EventSystem
        if (ServiceRegistry.Get<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Save scene
        string scenePath = "Assets/Scenes/WorldMap.unity";
        EnsureDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log($"WorldMap scene created at: {scenePath}");
    }

    #endregion

    #region Asset Creation

    [MenuItem("Window/AWITP/Create Sample Tutorial World")]
    public static void CreateSampleTutorialWorld()
    {
        EnsureDirectory("Assets/Resources/Levels/Worlds");
        EnsureDirectory("Assets/Resources/Levels/LevelDefinitions");

        // Create Tutorial World
        WorldData tutorialWorld = ScriptableObject.CreateInstance<WorldData>();
        tutorialWorld.worldId = "world_tutorial";
        tutorialWorld.worldName = "Tutorial Island";
        tutorialWorld.description = "Learn the basics of guiding Lems to their destination.";
        tutorialWorld.orderInGame = 0;
        tutorialWorld.themeColor = new Color(0.4f, 0.8f, 0.4f);

        // Create 5 tutorial levels
        LevelDefinition[] levels = new LevelDefinition[5];

        for (int i = 0; i < 5; i++)
        {
            levels[i] = ScriptableObject.CreateInstance<LevelDefinition>();
            levels[i].levelId = $"tutorial_{i + 1:D2}";
            levels[i].levelName = GetTutorialLevelName(i);
            levels[i].worldId = "world_tutorial";
            levels[i].orderInWorld = i;
            levels[i].gridWidth = 10;
            levels[i].gridHeight = 10;
            levels[i].levelDataJson = CreateBasicLevelJson(i);

            string levelPath = $"Assets/Resources/Levels/LevelDefinitions/{levels[i].levelId}.asset";
            AssetDatabase.CreateAsset(levels[i], levelPath);
        }

        tutorialWorld.levels = levels;

        string worldPath = "Assets/Resources/Levels/Worlds/World_Tutorial.asset";
        AssetDatabase.CreateAsset(tutorialWorld, worldPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created Tutorial World with {levels.Length} levels");
    }

    private static string GetTutorialLevelName(int index)
    {
        string[] names = {
            "First Steps",
            "Bridge Builder",
            "The Gap",
            "Higher Ground",
            "The Crossing"
        };
        return index < names.Length ? names[index] : $"Level {index + 1}";
    }

    private static string CreateBasicLevelJson(int levelIndex)
    {
        // Create a basic level structure
        LevelData data = new LevelData
        {
            gridWidth = 10,
            gridHeight = 10,
            levelName = GetTutorialLevelName(levelIndex)
        };

        // Add some basic content based on level index
        // Level 0: Simple bridge
        // Add permanent blocks for a platform
        for (int x = 0; x < 3; x++)
        {
            data.permanentBlocks.Add(new LevelData.BlockData(BlockType.Walk, x + 10)); // y=1
        }

        for (int x = 6; x < 10; x++)
        {
            data.permanentBlocks.Add(new LevelData.BlockData(BlockType.Walk, x + 10)); // y=1
        }

        // Add placeable spaces for the gap
        for (int x = 3; x < 6; x++)
        {
            data.placeableSpaceIndices.Add(x + 10); // y=1
        }

        // Add a Lem
        data.lems.Add(new LevelData.LemData(0 + 20, true)); // Start on left platform

        // Add inventory
        data.inventoryEntries.Add(new BlockInventoryEntry
        {
            entryId = "Walk",
            blockType = BlockType.Walk,
            displayName = "Platform",
            maxCount = 5,
            currentCount = 5
        });

        return JsonUtility.ToJson(data, true);
    }

    [MenuItem("Window/AWITP/Create Level Definition Template")]
    public static void CreateLevelDefinitionTemplate()
    {
        EnsureDirectory("Assets/Resources/Levels/LevelDefinitions");

        LevelDefinition template = ScriptableObject.CreateInstance<LevelDefinition>();
        template.levelId = "new_level";
        template.levelName = "New Level";
        template.worldId = "world_tutorial";
        template.orderInWorld = 0;
        template.gridWidth = 10;
        template.gridHeight = 10;

        // Create basic level data
        LevelData data = new LevelData
        {
            gridWidth = 10,
            gridHeight = 10,
            levelName = "New Level"
        };
        template.levelDataJson = JsonUtility.ToJson(data, true);

        string path = "Assets/Resources/Levels/LevelDefinitions/NewLevel_Template.asset";
        AssetDatabase.CreateAsset(template, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = template;
        EditorGUIUtility.PingObject(template);

        Debug.Log($"Created Level Definition template at: {path}");
    }

    #endregion

    #region Scene Configuration

    public static void AddVictoryScreenToScene()
    {
        // Find or create Canvas
        Canvas canvas = ServiceRegistry.Get<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create Victory Panel
        GameObject victoryPanel = CreateUIPanel(canvas.transform, "VictoryPanel");
        victoryPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        // Add content
        CreateUIText(victoryPanel.transform, "VictoryTitle", "Level Complete!", 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0, 0), new Vector2(500, 80));

        CreateUIText(victoryPanel.transform, "LevelNameText", "Level Name", 24, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0, 0), new Vector2(400, 40));

        CreateUIText(victoryPanel.transform, "TimeText", "Time: 00:00", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0, 0), new Vector2(200, 30));

        CreateUIText(victoryPanel.transform, "BlocksText", "Blocks: 0", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0, 0), new Vector2(200, 30));

        // Buttons
        GameObject nextBtn = CreateUIButton(victoryPanel.transform, "NextLevelButton", "Next Level",
            new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0, 0), new Vector2(180, 45));

        GameObject replayBtn = CreateUIButton(victoryPanel.transform, "ReplayButton", "Replay",
            new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), new Vector2(0, 0), new Vector2(180, 45));

        GameObject worldMapBtn = CreateUIButton(victoryPanel.transform, "WorldMapButton", "World Map",
            new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), new Vector2(0, 0), new Vector2(180, 45));

        // Add VictoryScreen component (use type lookup to avoid assembly reference issues)
        System.Type victoryScreenType = System.Type.GetType("VictoryScreen");
        if (victoryScreenType == null)
        {
            Debug.LogError("Could not find VictoryScreen type. Make sure VictoryScreen.cs exists.");
            return;
        }
        Component victoryScreen = victoryPanel.AddComponent(victoryScreenType);

        // Wire up references
        SerializedObject so = new SerializedObject(victoryScreen);
        so.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
        so.FindProperty("victoryTitleText").objectReferenceValue = victoryPanel.transform.Find("VictoryTitle").GetComponent<Text>();
        so.FindProperty("levelNameText").objectReferenceValue = victoryPanel.transform.Find("LevelNameText").GetComponent<Text>();
        so.FindProperty("timeText").objectReferenceValue = victoryPanel.transform.Find("TimeText").GetComponent<Text>();
        so.FindProperty("blocksText").objectReferenceValue = victoryPanel.transform.Find("BlocksText").GetComponent<Text>();
        so.FindProperty("nextLevelButton").objectReferenceValue = nextBtn.GetComponent<Button>();
        so.FindProperty("replayButton").objectReferenceValue = replayBtn.GetComponent<Button>();
        so.FindProperty("worldMapButton").objectReferenceValue = worldMapBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // Start hidden
        victoryPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Added VictoryScreen to current scene");
    }

    public static void AddLevelSystemManagers()
    {
        // Create Managers container
        GameObject managers = GameObject.Find("LevelSystemManagers");
        if (managers == null)
        {
            managers = new GameObject("LevelSystemManagers");
        }

        // Add LevelManager
        System.Type levelManagerType = System.Type.GetType("LevelManager");
        if (levelManagerType != null && UnityEngine.Object.FindAnyObjectByType(levelManagerType) == null)
        {
            managers.AddComponent(levelManagerType);
            Debug.Log("Added LevelManager");
        }

        // Add WorldManager
        System.Type worldManagerType = System.Type.GetType("WorldManager");
        if (worldManagerType != null && UnityEngine.Object.FindAnyObjectByType(worldManagerType) == null)
        {
            managers.AddComponent(worldManagerType);
            Debug.Log("Added WorldManager");
        }

        // Add ProgressManager
        System.Type progressManagerType = System.Type.GetType("ProgressManager");
        if (progressManagerType != null && UnityEngine.Object.FindAnyObjectByType(progressManagerType) == null)
        {
            managers.AddComponent(progressManagerType);
            Debug.Log("Added ProgressManager");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Level System Managers added to scene");
    }

    #endregion

    #region Build Settings

    public static void AddScenesToBuildSettings()
    {
        string[] scenePaths = {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/WorldMap.unity",
            "Assets/Scenes/SampleScene.unity"
        };

        var existingScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (string path in scenePaths)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Scene not found: {path}");
                continue;
            }

            bool alreadyAdded = existingScenes.Exists(s => s.path == path);
            if (!alreadyAdded)
            {
                existingScenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"Added scene to build: {path}");
            }
        }

        EditorBuildSettings.scenes = existingScenes.ToArray();
        Debug.Log("Build settings updated");
    }

    #endregion

    #region UI Helpers

    private static GameObject CreateUIPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        return panel;
    }

    private static void CreateUIText(Transform parent, string name, string text, int fontSize, TextAnchor anchor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        Text textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.alignment = anchor;
        textComp.color = Color.white;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static GameObject CreateUIButton(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.5f, 0.8f);

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.7f);
        button.colors = colors;

        // Add text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.fontSize = 18;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return buttonObj;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    #endregion
}
#endif
