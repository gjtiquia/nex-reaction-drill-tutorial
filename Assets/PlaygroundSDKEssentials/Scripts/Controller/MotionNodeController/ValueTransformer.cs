#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nex.Essentials.MotionNode
{
    [Serializable]
    public abstract class ValueTransformer : ValueManipulatorBase
    {
        protected abstract Vector2 Transform(Vector2 value);

        public Vector2 Before { get; private set; }
        public Vector2 After { get; private set; }

        public Vector2 Process(Vector2 value)
        {
            Before = value;
            return After = Transform(value);
        }

        public virtual void Start()
        {
        }

        public virtual void Update()
        {
        }
    }

    [Serializable]
    public class Absolute : ValueTransformer
    {
        protected override Vector2 Transform(Vector2 value)
        {
            return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
        }
    }

    [Serializable]
    public class Offset : ValueTransformer
    {
        [SerializeField] private Vector2 offset;

        protected override Vector2 Transform(Vector2 value)
        {
            return value + offset;
        }
    }

    [Serializable]
    public class Multiply : ValueTransformer
    {
        [SerializeField] private Vector2 multiplier;

        protected override Vector2 Transform(Vector2 value)
        {
            return value * multiplier;
        }
    }

    [Serializable]
    public class Normalize : ValueTransformer
    {
        protected override Vector2 Transform(Vector2 value)
        {
            return value.normalized;
        }
    }

    [Serializable]
    public class OneEuroFilter : ValueTransformer
    {
        [SerializeField] private OneEuroFilterConfig oneEuroFilterConfig = new(0.1f, 0.002f, 3);

        private Vector2? lastValue;
        private OneEuroFilterVector2 positionFilter = null!;

        protected override Vector2 Transform(Vector2 value)
        {
            lastValue = value;
            var output = positionFilter.FilteredValue ?? Vector2.zero;

            return output;
        }

        public override void Start()
        {
            positionFilter = new OneEuroFilterVector2(oneEuroFilterConfig);
        }

        public override void Update()
        {
            positionFilter.Update(lastValue, Time.unscaledDeltaTime);
        }
    }

    [Serializable]
    public class Zone
    {
        public enum ZoneShape
        {
            None = 0,
            Rectangle = 1,
            Circle = 2,
            RoundedRectangle = 3,
        }

        public ZoneShape shape = ZoneShape.None;
        public Vector2 minValue = -Vector2.one;
        public Vector2 maxValue = Vector2.one;
        public Vector2 zoneCenter = Vector2.zero;
        public Vector2 zoneSize = Vector2.zero;
        public float zoneRadius;
        public float cornerRadius;

        private bool HasShape => shape != ZoneShape.None;
        private bool UseMinMax => !HasShape;

        public bool Contains(Vector2 point)
        {
            return Clamp(point) == point;
        }

        public Vector2 Clamp(Vector2 point)
        {
            switch (shape)
            {
                case ZoneShape.None:
                {
                    var x = Mathf.Clamp(point.x, minValue.x, maxValue.x);
                    var y = Mathf.Clamp(point.y, minValue.y, maxValue.y);
                    return new Vector2(x, y);
                }
                case ZoneShape.Rectangle:
                {
                    var bounds = new Bounds(zoneCenter, zoneSize);
                    var x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
                    var y = Mathf.Clamp(point.y, bounds.min.y, bounds.max.y);
                    return new Vector2(x, y);
                }
                case ZoneShape.Circle:
                    var pointRadius = (point - zoneCenter);
                    if (pointRadius.magnitude <= zoneRadius)
                    {
                        return point;
                    }

                    return (pointRadius.normalized * zoneRadius);
                case ZoneShape.RoundedRectangle:
                {
                    var bounds = new Bounds(zoneCenter, zoneSize);
                    // Firstly, clamp by outer rectangle
                    var x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
                    var y = Mathf.Clamp(point.y, bounds.min.y, bounds.max.y);
                    var newPoint = new Vector2(x, y);

                    // Secondly, clamp by corner circles
                    var innerRect = new Bounds(zoneCenter, zoneSize - cornerRadius * 2 * Vector2.one);
                    var cornerCircleCenters = new List<Vector2>
                    {
                        new Vector2(innerRect.min.x, innerRect.min.y),
                        new Vector2(innerRect.min.x, innerRect.max.y),
                        new Vector2(innerRect.max.x, innerRect.min.y),
                        new Vector2(innerRect.max.x, innerRect.max.y),
                    };
                    var cornerSquareSize = cornerRadius * Vector2.one;
                    var cornerSquares = new List<Bounds>
                    {
                        new Bounds(cornerCircleCenters[0] + new Vector2(-cornerRadius / 2, -cornerRadius / 2),
                            cornerSquareSize),
                        new Bounds(cornerCircleCenters[1] + new Vector2(-cornerRadius / 2, cornerRadius / 2),
                            cornerSquareSize),
                        new Bounds(cornerCircleCenters[2] + new Vector2(cornerRadius / 2, -cornerRadius / 2),
                            cornerSquareSize),
                        new Bounds(cornerCircleCenters[3] + new Vector2(cornerRadius / 2, cornerRadius / 2),
                            cornerSquareSize),
                    };
                    for (var i = 0; i < cornerSquares.Count; i++)
                    {
                        var cornerSquare = cornerSquares[i];
                        if (cornerSquare.Contains(newPoint))
                        {
                            // If 𝑃 falls in one of those squares, compute the distance from 𝑃 to the corner of the square contained in the interior of 𝑅.
                            // If that distance is greater than 𝑟, then 𝑃∉𝑅′. Otherwise, 𝑃∈𝑅′.
                            var cornerCircleCenter = cornerCircleCenters[i];
                            var vec = newPoint - cornerCircleCenter;
                            if ((vec).magnitude > cornerRadius)
                            {
                                newPoint = cornerCircleCenter + vec.normalized * cornerRadius;
                                return newPoint;
                            }
                        }
                    }

                    return newPoint;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Vector2 GetCenter()
        {
            return UseMinMax ? new Vector2((maxValue.x + minValue.x) / 2f, (maxValue.y + minValue.y) / 2f) : zoneCenter;
        }
    }

    [Serializable]
    public class ClampZone : ValueTransformer
    {
        [SerializeField] private Zone clampZone = new Zone();

        protected override Vector2 Transform(Vector2 inputValue)
        {
            var outputValue = inputValue;
            outputValue = clampZone.Clamp(outputValue);
            return outputValue;
        }
    }

    [Serializable]
    public class DeadZone : ValueTransformer
    {
        public enum DeadZoneBehavior
        {
            SnapToCenter = 0,

            // StayOnBoundary = 1,
            Ignore = 2,
        }

        [SerializeField] private Zone deadZone = new Zone();

        [SerializeField] private DeadZoneBehavior deadZoneBehavior = DeadZoneBehavior.Ignore;
        public Vector3? LastValue { get; private set; }

        protected override Vector2 Transform(Vector2 inputValue)
        {
            var outputValue = inputValue;
            var inDeadZone = deadZone.Contains(outputValue);
            if (inDeadZone)
            {
                switch (deadZoneBehavior)
                {
                    case DeadZoneBehavior.SnapToCenter:
                        outputValue = deadZone.GetCenter();
                        break;
                    // case DeadZoneBehavior.StayOnBoundary:
                    //     // TODO
                    //     break;
                    case DeadZoneBehavior.Ignore:
                        if (LastValue is { } value)
                        {
                            outputValue = value;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            LastValue = outputValue;
            return outputValue;
        }
    }

    [Serializable]
    public class SmoothDamp : ValueTransformer
    {
        [SerializeField] private float smoothTime;
        [SerializeField] private float maxSpeed;

        private Vector2 lastValue = Vector2.zero;
        private Vector2 velocityRef = Vector2.zero;

        protected override Vector2 Transform(Vector2 value)
        {
            lastValue = Vector2.SmoothDamp(lastValue, value, ref velocityRef, smoothTime, maxSpeed);
            return lastValue;
        }
    }

    /// Swap x and y axis
    [Serializable]
    public class RemapAxis : ValueTransformer
    {
        protected override Vector2 Transform(Vector2 value)
        {
            return new Vector2(value.y, value.x);
        }
    }

    /// Snap/rotate the input vector to a given axis, retaining the magnitude
    [Serializable]
    public class SnapToAxis : ValueTransformer
    {
        [SerializeField] private Vector2 axis;

        protected override Vector2 Transform(Vector2 value)
        {
            if (axis == Vector2.zero)
            {
                return Vector2.zero;
            }

            var sign = Vector2.Dot(axis, value) > 0 ? 1 : -1;
            return sign * value.magnitude * axis.normalized;
        }
    }
}
