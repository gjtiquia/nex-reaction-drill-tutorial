#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Nex.Essentials
{
    public enum JoystickFourDirections
    {
        Neutral,
        Up,
        Right,
        Down,
        Left,
    }

    public enum JoystickEightDirections
    {
        Neutral,
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft,
    }

    public readonly struct JoystickOutput
    {
        private readonly Vector2 rawVector;
        private readonly Vector2 outputVector;

        public JoystickOutput(Vector2 outputVector, Vector2 rawVector)
        {
            this.outputVector = outputVector;
            this.rawVector = rawVector;
        }

        /// <summary>
        /// Gets the current joystick input as a vector.
        /// </summary>
        public Vector2 GetJoystickPosition() => outputVector;

        /// <summary>
        /// Gets the current raw joystick input as a vector, without considering the dead zone.
        /// </summary>
        public Vector2 GetRawJoystickPosition() => rawVector;

        /// <summary>
        /// Gets the current joystick input direction in the up, down, left, right directions.
        /// </summary>
        public JoystickFourDirections GetFourDirection()
        {
            if (outputVector == Vector2.zero) return JoystickFourDirections.Neutral;

            var angle = Vector2.SignedAngle(Vector2.up, outputVector);

            return angle switch
            {
                > -45 and <= 45 => JoystickFourDirections.Up,
                > 45 and <= 135 => JoystickFourDirections.Left,
                > 135 or <= -135 => JoystickFourDirections.Down,
                > -135 and <= -45 => JoystickFourDirections.Right,
                _ => JoystickFourDirections.Neutral
            };
        }

        /// <summary>
        /// Gets the current joystick input direction in the up, down, left, right, and diagonal directions.
        /// </summary>
        public JoystickEightDirections GetEightDirection()
        {
            if (outputVector == Vector2.zero) return JoystickEightDirections.Neutral;

            var angle = Vector2.SignedAngle(Vector2.up, outputVector);

            return angle switch
            {
                > -22.5f and <= 22.5f => JoystickEightDirections.Up,
                > 22.5f and <= 67.5f => JoystickEightDirections.UpLeft,
                > 67.5f and <= 112.5f => JoystickEightDirections.Left,
                > 112.5f and <= 157.5f => JoystickEightDirections.DownLeft,
                > 157.5f or <= -157.5f => JoystickEightDirections.Down,
                > -157.5f and <= -112.5f => JoystickEightDirections.DownRight,
                > -112.5f and <= -67.5f => JoystickEightDirections.Right,
                > -67.5f and <= -22.5f => JoystickEightDirections.UpRight,
                _ => JoystickEightDirections.Neutral
            };
        }

        /// <summary>
        /// Gets the current joystick input in the up, down, left, right directions as a scaled vector.
        /// </summary>
        public Vector2 GetFourDirectionVector()
        {
            var direction = GetFourDirection();
            return direction.ToVector2() * outputVector.magnitude;
        }

        /// <summary>
        /// Gets the current joystick input in the up, down, left, right, and diagonal directions as a scaled vector.
        /// </summary>
        public Vector2 GetEightDirectionVector()
        {
            var direction = GetEightDirection();
            return direction.ToVector2() * outputVector.magnitude;
        }

        /// <summary>
        /// Gets the current joystick input in the horizontal direction in the range [-1, 1].
        /// </summary>
        public float GetHorizontalAxis() => outputVector.x;

        /// <summary>
        /// Gets the current joystick input in the vertical direction in the range [-1, 1].
        /// </summary>
        public float GetVerticalAxis() => outputVector.y;
    }

    public static class JoystickDirectionsExtensions
    {
        private const float Sqrt2 = 0.7071067812f;

        public static Vector2 ToVector2(this JoystickFourDirections direction) =>
            direction switch
            {
                JoystickFourDirections.Neutral => new Vector2(0, 0),
                JoystickFourDirections.Up => new Vector2(0, 1),
                JoystickFourDirections.Right => new Vector2(1, 0),
                JoystickFourDirections.Down => new Vector2(0, -1),
                JoystickFourDirections.Left => new Vector2(-1, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };

        public static Vector2 ToVector2(this JoystickEightDirections direction) =>
            direction switch
            {
                JoystickEightDirections.Neutral => new Vector2(0, 0),
                JoystickEightDirections.Up => new Vector2(0, 1),
                JoystickEightDirections.UpRight => new Vector2(Sqrt2, Sqrt2),
                JoystickEightDirections.Right => new Vector2(1, 0),
                JoystickEightDirections.DownRight => new Vector2(Sqrt2, -Sqrt2),
                JoystickEightDirections.Down => new Vector2(0, -1),
                JoystickEightDirections.DownLeft => new Vector2(-Sqrt2, -Sqrt2),
                JoystickEightDirections.Left => new Vector2(-1, 0),
                JoystickEightDirections.UpLeft => new Vector2(-Sqrt2, Sqrt2),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
    }

    /// <summary>
    /// Produces joystick-like input based on cursor position, with configurable dead zone and output normalization.
    /// </summary>
    public class JoystickProducer : MonoBehaviour
    {
        [SerializeField] private CursorProducer cursorProducer = null!;

        [SerializeField, Range(0f, 1f), Tooltip("Minimum distance from center before input is registered")]
        private float deadZone = 0.1f;

        [SerializeField, Tooltip("Sensitivity to the x, y coordinates. If you want it to be less sensitive on y, use (1, 0.5)")]
        private Vector2 sensitivity = Vector2.one;

        public enum Scale
        {
            Linear,
            Squared
        }

        [SerializeField, Tooltip("The scale of the joystick output vector")]
        private Scale scale = Scale.Squared;

        public float DeadZone { get => deadZone; set => deadZone = Mathf.Clamp01(value); }
        public Scale OutputScale { get => scale; set => scale = value; }

        public Vector2 Sensitivity { get => sensitivity; set => sensitivity = value; }

        private readonly AsyncReactiveProperty<JoystickOutput> output = new(new JoystickOutput());

        /// <summary>
        /// Gets the current joystick output value.
        /// </summary>
        public JoystickOutput JoystickOutput => output.Value;

        /// <summary>
        /// Gets a stream of joystick output values that updates whenever the input changes.
        /// </summary>
        public IUniTaskAsyncEnumerable<JoystickOutput> JoystickOutputStream => output;

        private void Start()
        {
            cursorProducer.CursorPositionStream.Subscribe(HandleCursor, this.GetCancellationTokenOnDestroy());
        }

        private void HandleCursor(Vector2? position)
        {
            if (position == null)
            {
                UpdateOutput(Vector2.zero);
                return;
            }

            // Scale the position from the range of [-0.5, 0.5] to the range [-1, 1]
            var scaledPosition = position.Value * 2f * sensitivity;

            UpdateOutput(scaledPosition);
        }

        private void UpdateOutput(Vector2 rawVector)
        {
            // Scale the raw vector magnitude to the range [-1, 1], where 0 means the dead zone
            var rawMagnitude = rawVector.magnitude;
            var outputVector = rawMagnitude < deadZone
                ? Vector2.zero
                : Mathf.InverseLerp(deadZone, 1, rawMagnitude) / rawMagnitude * rawVector;

            if (scale == Scale.Squared)
            {
                outputVector = outputVector.magnitude * outputVector;
            }

            if (output.Value.GetJoystickPosition() == outputVector &&
                output.Value.GetRawJoystickPosition() == rawVector)
                return;

            output.Value = new JoystickOutput(Vector2.ClampMagnitude(outputVector, 1),
                Vector2.ClampMagnitude(rawVector, 1));
        }
    }
}
