using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared utility for parsing transporter route strings.
/// Used by both TransporterBlock and GridManager to avoid code duplication.
/// </summary>
public static class RouteParser
{
    private static readonly char[] RouteSeparators = { ',', '_', ' ' };

    /// <summary>
    /// Represents a parsed route, including normalized tokens, expanded steps, and any validation error.
    /// </summary>
    public readonly struct RouteData
    {
        public readonly string[] normalizedSteps;
        public readonly List<Vector2Int> expandedSteps;
        public readonly string error;

        public bool IsValid => string.IsNullOrEmpty(error);

        public RouteData(string[] normalizedSteps, List<Vector2Int> expandedSteps, string error)
        {
            this.normalizedSteps = normalizedSteps ?? System.Array.Empty<string>();
            this.expandedSteps = expandedSteps ?? new List<Vector2Int>();
            this.error = error;
        }
    }

    /// <summary>
    /// Parses route step strings (e.g., "L2", "U1", "R3") into directional vectors.
    /// Each step like "L2" expands into 2 individual Vector2Int(-1, 0) entries.
    /// </summary>
    /// <param name="routeSteps">Array of route tokens like ["L2", "U1", "R3"]</param>
    /// <returns>List of individual step vectors</returns>
    public static List<Vector2Int> ParseRouteSteps(string[] routeSteps)
    {
        return ParseRoute(routeSteps, null).expandedSteps;
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
        string[] normalized = NormalizeRouteSteps(routeSteps);
        if (normalized == null || normalized.Length == 0) return "";
        return string.Join("_", normalized);
    }

    /// <summary>
    /// Normalizes route tokens by trimming, uppercasing, and removing invalid tokens.
    /// </summary>
    public static string[] NormalizeRouteSteps(string[] routeSteps)
    {
        RouteData data = ParseRoute(routeSteps, null);
        return data.normalizedSteps;
    }

    /// <summary>
    /// Parses and validates a route using routeSteps or a fallback route string.
    /// </summary>
    public static RouteData ParseRoute(string[] routeSteps, string fallbackRoute)
    {
        string[] tokens = ResolveRouteTokens(routeSteps, fallbackRoute);
        List<Vector2Int> steps = new List<Vector2Int>();
        List<string> normalized = new List<string>();
        string error = null;

        if (tokens == null || tokens.Length == 0)
        {
            return new RouteData(System.Array.Empty<string>(), steps, null);
        }

        foreach (string raw in tokens)
        {
            if (!TryParseRouteToken(raw, out char dir, out int count))
            {
                if (error == null && !string.IsNullOrWhiteSpace(raw))
                {
                    error = $"Invalid route step '{raw}'. Expected format like L2, U1, R3, D4.";
                }
                continue;
            }

            normalized.Add($"{dir}{count}");
            Vector2Int step = DirectionToVector(dir);
            for (int i = 0; i < count; i++)
            {
                steps.Add(step);
            }
        }

        if (normalized.Count == 0 && error == null)
        {
            error = "Route contains no valid steps.";
        }

        return new RouteData(normalized.ToArray(), steps, error);
    }

    /// <summary>
    /// Resolves route tokens from explicit steps or a fallback string.
    /// </summary>
    public static string[] ResolveRouteTokens(string[] routeSteps, string fallbackRoute)
    {
        if (routeSteps != null && routeSteps.Length > 0)
        {
            return routeSteps;
        }

        if (!string.IsNullOrWhiteSpace(fallbackRoute))
        {
            return fallbackRoute.Split(RouteSeparators, System.StringSplitOptions.RemoveEmptyEntries);
        }

        return null;
    }

    private static bool TryParseRouteToken(string raw, out char dir, out int count)
    {
        dir = '\0';
        count = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string token = raw.Trim().ToUpperInvariant();
        if (token.Length < 2) return false;

        dir = token[0];
        if (dir != 'L' && dir != 'R' && dir != 'U' && dir != 'D') return false;
        if (!int.TryParse(token.Substring(1), out count)) return false;
        return count > 0;
    }

    private static Vector2Int DirectionToVector(char dir)
    {
        return dir switch
        {
            'L' => new Vector2Int(-1, 0),
            'R' => new Vector2Int(1, 0),
            'U' => new Vector2Int(0, 1),
            'D' => new Vector2Int(0, -1),
            _ => Vector2Int.zero
        };
    }
}
