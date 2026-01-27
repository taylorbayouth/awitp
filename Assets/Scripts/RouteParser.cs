using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared utility for parsing transporter route strings.
/// Used by both TransporterBlock and GridManager to avoid code duplication.
/// </summary>
public static class RouteParser
{
    /// <summary>
    /// Parses route step strings (e.g., "L2", "U1", "R3") into directional vectors.
    /// Each step like "L2" expands into 2 individual Vector2Int(-1, 0) entries.
    /// </summary>
    /// <param name="routeSteps">Array of route tokens like ["L2", "U1", "R3"]</param>
    /// <returns>List of individual step vectors</returns>
    public static List<Vector2Int> ParseRouteSteps(string[] routeSteps)
    {
        List<Vector2Int> steps = new List<Vector2Int>();
        if (routeSteps == null) return steps;

        foreach (string raw in routeSteps)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string token = raw.Trim().ToUpperInvariant();
            if (token.Length < 2) continue;

            char dir = token[0];
            if (!int.TryParse(token.Substring(1), out int count) || count <= 0) continue;

            Vector2Int step = dir switch
            {
                'L' => new Vector2Int(-1, 0),
                'R' => new Vector2Int(1, 0),
                'U' => new Vector2Int(0, 1),
                'D' => new Vector2Int(0, -1),
                _ => Vector2Int.zero
            };

            if (step == Vector2Int.zero) continue;

            for (int i = 0; i < count; i++)
            {
                steps.Add(step);
            }
        }

        return steps;
    }

    /// <summary>
    /// Reverses a list of route steps (for return journey).
    /// </summary>
    public static List<Vector2Int> ReverseSteps(List<Vector2Int> steps)
    {
        List<Vector2Int> reversed = new List<Vector2Int>(steps.Count);
        for (int i = steps.Count - 1; i >= 0; i--)
        {
            Vector2Int step = steps[i];
            reversed.Add(new Vector2Int(-step.x, -step.y));
        }
        return reversed;
    }

    /// <summary>
    /// Normalizes a route string array for use as an inventory key.
    /// </summary>
    public static string NormalizeRouteKey(string[] routeSteps)
    {
        if (routeSteps == null || routeSteps.Length == 0) return "";
        return string.Join("_", routeSteps).ToUpperInvariant();
    }
}
