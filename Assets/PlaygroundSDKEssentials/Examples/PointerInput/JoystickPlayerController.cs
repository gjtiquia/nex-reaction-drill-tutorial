using System;
using UnityEngine;

namespace Nex.Essentials.Examples.PointerInput
{
    public class JoystickPlayerController : MonoBehaviour
    {
        [SerializeField] private JoystickProducer joystickProducer = null!;
        [SerializeField] private float speed = 1;
        [SerializeField] private float maxX;
        [SerializeField] private float maxY;

        private enum Mode
        {
            AllDirections,
            FourDirections,
            EightDirections,
            HorizontalOnly,
            VerticalOnly
        }

        [SerializeField] private Mode mode = Mode.AllDirections;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
        }

        private void Update()
        {
            var direction = GetMovement();
            var delta = speed * Time.deltaTime * direction;
            MovePlayer(delta);
        }

        private Vector2 GetMovement() => mode switch
        {
            Mode.AllDirections => joystickProducer.JoystickOutput.GetJoystickPosition(),
            Mode.FourDirections => joystickProducer.JoystickOutput.GetFourDirectionVector(),
            Mode.EightDirections => joystickProducer.JoystickOutput.GetEightDirectionVector(),
            Mode.HorizontalOnly => new Vector2(joystickProducer.JoystickOutput.GetHorizontalAxis(), 0),
            Mode.VerticalOnly => new Vector2(0, joystickProducer.JoystickOutput.GetVerticalAxis()),
            _ => throw new ArgumentOutOfRangeException()
        };

        private void MovePlayer(Vector2 delta)
        {
            // Clamp range of motion
            var position = rectTransform.anchoredPosition;
            position.x = Mathf.Clamp(position.x + delta.x, -maxX, maxX);
            position.y = Mathf.Clamp(position.y + delta.y, -maxY, maxY);
            rectTransform.anchoredPosition = position;
        }
    }
}
