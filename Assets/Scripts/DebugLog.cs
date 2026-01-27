using UnityEngine;

public static class DebugLog
{
    public static bool InfoEnabled = true;
    public static bool CrumblerEnabled = true;

    public static void Info(string message, Object context = null)
    {
        if (!InfoEnabled) return;
        if (context != null)
        {
            Debug.Log(message, context);
        }
        else
        {
            Debug.Log(message);
        }
    }

    public static void Crumbler(string message, Object context = null)
    {
        if (!CrumblerEnabled) return;
        if (context != null)
        {
            Debug.Log(message, context);
        }
        else
        {
            Debug.Log(message);
        }
    }
}
