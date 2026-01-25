/// <summary>
/// Defines the different game modes.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Editor: Player places blocks to create an obstacle course.
    /// </summary>
    Editor,

    /// <summary>
    /// Play: Lem walks and physics runs. Test your course!
    /// </summary>
    Play,

    /// <summary>
    /// Level Editor (Designer Mode): Place Lem and mark placeable spaces.
    /// Press E to toggle this mode.
    /// </summary>
    LevelEditor
}
