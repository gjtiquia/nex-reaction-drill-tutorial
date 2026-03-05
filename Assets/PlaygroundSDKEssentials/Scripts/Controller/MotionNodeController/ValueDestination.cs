#nullable enable

using System;
using Unity.Mathematics;
using UnityEngine;

namespace Nex.Essentials.MotionNode
{
    [Serializable]
    public abstract class ValueDestination : ValueManipulatorBase
    {
        public abstract void Apply(Vector2 input);
    }

    [Serializable]
    public class WorldPosition : ValueDestination
    {
        public float zOverride;
        public Transform target = null!;
        public bool3 applyToAxis = new(true);

        public override void Apply(Vector2 input)
        {
            target.localPosition = new Vector3(applyToAxis.x ? input.x : target.localPosition.x,
                applyToAxis.y ? input.y : target.localPosition.y, applyToAxis.z ? zOverride : target.localPosition.z);
        }
    }

    [Serializable]
    public class UIAnchorPosition : ValueDestination
    {
        public RectTransform target = null!;
        public bool2 applyToAxis = new(true);

        public override void Apply(Vector2 input)
        {
            target.anchoredPosition = new Vector2(applyToAxis.x ? input.x : target.anchoredPosition.x,
                applyToAxis.y ? input.y : target.anchoredPosition.y);
        }
    }

    [Serializable]
    public class UIPivotPosition : ValueDestination
    {
        public RectTransform target = null!;
        public bool2 applyToAxis = new(true);

        public override void Apply(Vector2 input)
        {
            target.pivot = new Vector2(applyToAxis.x ? input.x : target.pivot.x,
                applyToAxis.y ? input.y : target.pivot.y);
        }
    }

    [Serializable]
    public class LocalRotationAngles : ValueDestination
    {
        public enum Axis2
        {
            X = 0,
            Y = 1,
        }

        public Transform target = null!;
        public bool3 applyToAxis = new(false);
        public Axis2 xRotationFrom = Axis2.Y;
        public Axis2 yRotationFrom = Axis2.X;
        public Axis2 zRotationFrom = Axis2.X;

        public override void Apply(Vector2 value)
        {
            var currentRotation = target.localEulerAngles;

            var eulerAngles = new Vector3(
                applyToAxis.x ?
                    GetInputValue(value, xRotationFrom)
                    : currentRotation.x,
                applyToAxis.y ?
                    GetInputValue(value, yRotationFrom)
                    : currentRotation.y,
                applyToAxis.z ?
                    GetInputValue(value, zRotationFrom)
                    : currentRotation.z
            );

            target.transform.localRotation = Quaternion.Euler(eulerAngles);
        }

        private float GetInputValue(Vector2 input, Axis2 axis) => axis switch
        {
            Axis2.X => input.x,
            Axis2.Y => input.y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
