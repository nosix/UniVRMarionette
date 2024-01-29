using System;
using UnityEngine;

namespace VRMarionette
{
    public enum Direction
    {
        XAxis,
        YAxis,
        ZAxis,
    }

    public static class DirectionExtension
    {
        public static Vector3 ToAxis(this Direction direction)
        {
            return direction switch
            {
                Direction.XAxis => Vector3.right,
                Direction.YAxis => Vector3.up,
                Direction.ZAxis => Vector3.forward,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        public static Direction ToDirection(this int value)
        {
            return (Direction)value;
        }
    }
}