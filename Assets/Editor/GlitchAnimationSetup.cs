using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GlitchAnimationSetup
{
    private const string AnimationsFolder = "Assets/Glitch/Animations";
    private const string GlitchPrefabPath = "Assets/Glitch/Prefabs/PF_Glitch.prefab";
    private const string GlitchModelPath = "Assets/Glitch/Meshes/SK_Glitch.fbx";
    private const string ControllerPath = "Assets/Glitch/Animations/Glitch.controller";

    [MenuItem("Tools/Glitch/Setup Animations")]
    public static void Setup()
    {
        EnsureGlitchModelHumanoid();
        Avatar avatar = LoadGlitchAvatar();
        if (avatar == null)
        {
            Debug.LogWarning("[GlitchSetup] Could not find Glitch avatar on SK_Glitch.fbx. Animations will use their own avatars.");
        }

        string[] fbxPaths =
        {
            "Assets/Glitch/Animations/Happy Walk.fbx",
            "Assets/Glitch/Animations/Walking Carrying.fbx",
            "Assets/Glitch/Animations/Sad Idle.fbx",
        };

        foreach (string fbxPath in fbxPaths)
        {
            ConfigureAnimationFbx(fbxPath);
        }

        AssetDatabase.Refresh();
        foreach (string fbxPath in fbxPaths)
        {
            LogFbxClips(fbxPath);
        }

        AnimationClip idleClip = GetClipFromFbx("Assets/Glitch/Animations/Sad Idle.fbx") ?? FindClipByName("Idle");
        AnimationClip walkClip = GetClipFromFbx("Assets/Glitch/Animations/Happy Walk.fbx")
            ?? FindClipByName("Walk");
        AnimationClip carryWalkClip = GetClipFromFbx("Assets/Glitch/Animations/Walking Carrying.fbx")
            ?? FindClipByName("Carrying");

        if (idleClip == null || walkClip == null)
        {
            Debug.LogError("[GlitchSetup] Missing idle or walk clips. Check that the FBX imports have clips.");
            LogAvailableClips();
            return;
        }

        AnimatorController controller = CreateOrLoadController();
        if (controller == null)
        {
            Debug.LogError("[GlitchSetup] Failed to create AnimatorController.");
            return;
        }

        BuildStateMachine(controller, idleClip, walkClip, carryWalkClip);
        AssignControllerToPrefab(controller, avatar);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[GlitchSetup] Glitch animation setup complete.");
    }

    private static Avatar LoadGlitchAvatar()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(GlitchModelPath);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Avatar avatar)
            {
                return avatar;
            }
        }
        return null;
    }

    private static void ConfigureAnimationFbx(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[GlitchSetup] Could not find ModelImporter for {fbxPath}");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar = null;
        importer.importAnimation = true;
        if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
        {
            ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
            if (defaults != null && defaults.Length > 0)
            {
                importer.clipAnimations = defaults;
            }
        }
        ForceLoopClips(importer);
        importer.SaveAndReimport();
    }

    private static void ForceLoopClips(ModelImporter importer)
    {
        if (importer == null) return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        if (clips == null || clips.Length == 0) return;

        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = true;
            clips[i].loopPose = true;
        }

        importer.clipAnimations = clips;
    }

    private static void EnsureGlitchModelHumanoid()
    {
        ModelImporter importer = AssetImporter.GetAtPath(GlitchModelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("[GlitchSetup] Could not find ModelImporter for SK_Glitch.fbx.");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.SaveAndReimport();
    }

    private static AnimatorController CreateOrLoadController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null)
        {
            return controller;
        }

        return AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
    }

    private static void BuildStateMachine(AnimatorController controller, AnimationClip idleClip, AnimationClip walkClip, AnimationClip carryWalkClip)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Clear existing states/transitions for a clean setup
        foreach (var state in stateMachine.states.ToList())
        {
            stateMachine.RemoveState(state.state);
        }

        foreach (var transition in stateMachine.anyStateTransitions.ToList())
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = walkClip;

        AnimatorState carryState = null;
        if (carryWalkClip != null)
        {
            carryState = stateMachine.AddState("WalkCarry");
            carryState.motion = carryWalkClip;
        }


        stateMachine.defaultState = idleState;

        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "HasKey", AnimatorControllerParameterType.Bool);

        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.05f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "HasKey");

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        if (carryState != null)
        {
            AnimatorStateTransition toCarryFromIdle = idleState.AddTransition(carryState);
            toCarryFromIdle.hasExitTime = false;
            toCarryFromIdle.duration = 0.05f;
            toCarryFromIdle.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toCarryFromIdle.AddCondition(AnimatorConditionMode.If, 0f, "HasKey");

            AnimatorStateTransition toCarryFromWalk = walkState.AddTransition(carryState);
            toCarryFromWalk.hasExitTime = false;
            toCarryFromWalk.duration = 0.05f;
            toCarryFromWalk.AddCondition(AnimatorConditionMode.If, 0f, "HasKey");

            AnimatorStateTransition toWalkFromCarry = carryState.AddTransition(walkState);
            toWalkFromCarry.hasExitTime = false;
            toWalkFromCarry.duration = 0.05f;
            toWalkFromCarry.AddCondition(AnimatorConditionMode.IfNot, 0f, "HasKey");

            AnimatorStateTransition toIdleFromCarry = carryState.AddTransition(idleState);
            toIdleFromCarry.hasExitTime = false;
            toIdleFromCarry.duration = 0.05f;
            toIdleFromCarry.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in controller.parameters)
        {
            if (param.name == name)
            {
                return;
            }
        }

        controller.AddParameter(name, type);
    }

    private static AnimationClip FindClipByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            if (clip.name.IndexOf("__preview__", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return clip;
            }
        }
        return null;
    }

    private static AnimationClip GetClipFromFbx(string fbxPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (UnityEngine.Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip == null) continue;
            if (clip.name.IndexOf("__preview__", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            return clip;
        }

        return null;
    }

    private static void LogAvailableClips()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationsFolder });
        List<string> names = new List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            if (clip.name.IndexOf("__preview__", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            names.Add($"{clip.name} ({path})");
        }

        Debug.Log($"[GlitchSetup] Found clips: {(names.Count == 0 ? "none" : string.Join(", ", names))}");
    }

    private static void LogFbxClips(string fbxPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        List<string> clipNames = new List<string>();
        foreach (UnityEngine.Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip == null) continue;
            if (clip.name.IndexOf("__preview__", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            clipNames.Add(clip.name);
        }

        Debug.Log($"[GlitchSetup] Clips in {fbxPath}: {(clipNames.Count == 0 ? "none" : string.Join(", ", clipNames))}");
    }

    private static void AssignControllerToPrefab(AnimatorController controller, Avatar avatar)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlitchPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[GlitchSetup] PF_Glitch.prefab not found.");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("[GlitchSetup] Failed to instantiate PF_Glitch.prefab.");
            return;
        }

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[GlitchSetup] Animator missing on PF_Glitch.prefab.");
            UnityEngine.Object.DestroyImmediate(instance);
            return;
        }

        animator.runtimeAnimatorController = controller;
        if (avatar != null)
        {
            animator.avatar = avatar;
        }
        PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
        UnityEngine.Object.DestroyImmediate(instance);
    }
}
