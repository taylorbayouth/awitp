/// <summary>
/// Defines the different game modes.
/// Note: Renamed from "Editor/LevelEditor" to avoid confusion with Unity's Editor classes.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Builder: Player places blocks to create an obstacle course.
    /// </summary>
    Builder,

    /// <summary>
    /// Play: Lem walks and physics runs. Test your course!
    /// </summary>
    Play,

    /// <summary>
    /// Designer: Design level layout, place Lem and mark placeable spaces.
    /// Press E to toggle between Builder and Designer modes.
    /// </summary>
    Designer
}
