using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A static class for general helpful methods
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Destroy all child objects of this transform (Unintentionally evil sounding).
    /// Use it like so:
    /// <code>
    /// transform.DestroyChildren();
    /// </code>
    /// </summary>
    public static void DestroyChildren(this Transform t)
    {
        foreach (Transform child in t) UnityEngine.Object.Destroy(child.gameObject);
    }

    public static bool IsEqual(this Vector3 v1, Vector3 v2)
    {
        return Math.Abs(v1.x - v2.x) < 0.001f && Math.Abs(v1.y - v2.y) < 0.001f && Math.Abs(v1.z - v2.z) < 0.001f;
    }

    public static T[] ToArray<T>(IReadOnlyList<T> readOnlyList)
    {
        List<T> list = new();

        foreach (T element in readOnlyList)
        {
            list.Add(element);
        }

        return list.ToArray<T>();
    }
}
