/// <summary>
/// Centralized constants used across gameplay systems.
/// Keep shared keys/tags/paths here to avoid string duplication and drift.
/// </summary>
public static class GameConstants
{
    public static class Grid
    {
        public const float CellSize = 1f;
    }

    public static class Tags
    {
        public const string Player = "Player";
    }

    public static class PlayerPrefsKeys
    {
        public const string SelectedWorldId = "SelectedWorldId";
        public const string SelectedLevelId = "SelectedLevelId";
        public const string PendingLevelId = "PendingLevelId";
        /// <summary>Stores the worldId of a newly unlocked world to trigger a reveal animation in the overworld.</summary>
        public const string PendingWorldReveal = "PendingWorldReveal";
    }

    public static class ResourcePaths
    {
        public const string BaseBlockPrefab = "Blocks/BaseBlock";
        public const string LemMeshPrefab = "Characters/LemMeshPreRigged";
        public const string LemTexture = "Characters/LemMeshPreRigged_Albedo";
        public const string BuilderMusicTrack = "Music/SoundtrackBuild";
        public const string PlayMusicTrack = "Music/SoundtrackPlay";
        public const string LevelsRoot = "Levels";
        public const string LevelDefinitionsRoot = "Levels/LevelDefinitions";
        public const string WorldsRoot = "Levels/Worlds";
        public const string GreenApplePrefab = "GreenApple";
    }

    /// <summary>
    /// Animation clip identifiers and resource paths for the Playables-based animation system.
    /// To add a new animation: add constants here, then load + register in LemController.SetupVisual().
    /// </summary>
    public static class AnimationClips
    {
        // Clip identifiers (used with LemAnimationPlayables.SetActiveClip)
        public const string Idle = "idle";
        public const string Walk = "walk";
        public const string WalkCarry = "walkCarry";

        // Resource paths (relative to Resources/, no extension)
        public const string IdlePath = "Animations/Sad Idle";
        public const string WalkPath = "Animations/Sad Walk";
        public const string WalkCarryPath = "Animations/Walking Carrying";
    }

    public static class ObjectNames
    {
        public const string Sky = "Sky";
    }

    public static class SceneNames
    {
        public const string Gameplay = "Master";
        public const string MainMenu = "MainMenu";
        public const string WorldMap = "WorldMap";
        public const string LevelSelect = "LevelSelect";
        public const string Overworld = "Overworld";
    }

    public static class SaveFiles
    {
        public const string Progress = "progress.json";
    }

    public static class Defaults
    {
        public const string InitialWorldId = "onboarding";
    }
}
