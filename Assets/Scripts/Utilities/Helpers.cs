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

    public static Vector3 GetPointInDirection(this Vector3 startingPoint, Direction[] directions, float moveInterval = 5f)
    {
        Vector3 newPoint = startingPoint;

        foreach (Direction direction in directions)
        {
            if (direction == Direction.North)
            {
                newPoint.z -= moveInterval;
            }
            else if (direction == Direction.East)
            {
                newPoint.x -= moveInterval;
            }
            else if (direction == Direction.South)
            {
                newPoint.z += moveInterval;
            }
            else if (direction == Direction.West)
            {
                newPoint.x += moveInterval;
            }
        }

        return newPoint;
    }

    public static Direction GetOppositeDirection(this Direction direction)
    {
        Direction oppositeDirection = Direction.None;

        switch (direction)
        {
            case Direction.North:
                oppositeDirection = Direction.South;
                break;
            case Direction.East:
                oppositeDirection = Direction.West;
                break;
            case Direction.South:
                oppositeDirection = Direction.North;
                break;
            case Direction.West:
                oppositeDirection = Direction.East;
                break;
            default:
                break;
        }

        return oppositeDirection;
    }

    public static bool IsNextDirectionLeftTurn(this Direction direction, Direction nextDirection)
    {
        return direction == Direction.North && nextDirection == Direction.West || direction == Direction.East && nextDirection == Direction.North || direction == Direction.South && nextDirection == Direction.East || direction == Direction.West && nextDirection == Direction.South;
    }
}

public enum Direction
{
    North,
    East,
    South,
    West,
    None
}
