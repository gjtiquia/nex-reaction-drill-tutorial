#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Nex.Essentials.Examples.PointerInput
{
    public class TargetPointerController : MonoBehaviour
    {
        [SerializeField] private CursorProducer leftCursorProducer = null!;
        [SerializeField] private CursorProducer rightCursorProducer = null!;

        [Header("Player Display")]
        [SerializeField] private RectTransform leftPlayer = null!;
        [SerializeField] private RectTransform rightPlayer = null!;
        [SerializeField] private float scale = 1080;
        
        [Header("Delay")]
        [SerializeField] private float smoothTime = 0.5f;
        [SerializeField] private float maxSpeed = 10f;

        private Vector2 leftPlayerVelocity = Vector2.zero;
        private Vector2 rightPlayerVelocity = Vector2.zero;

        private void Start()
        {
            leftCursorProducer.CursorPositionStream.Subscribe(HandleLeftCursor, this.GetCancellationTokenOnDestroy());
            rightCursorProducer.CursorPositionStream.Subscribe(HandleRightCursor, this.GetCancellationTokenOnDestroy());
        }

        private void HandleLeftCursor(Vector2? position)
        {
            HandleCursor(position, leftPlayer, ref leftPlayerVelocity);
        }

        private void HandleRightCursor(Vector2? position)
        {
            HandleCursor(position, rightPlayer, ref rightPlayerVelocity);
        }

        private void HandleCursor(Vector2? position, RectTransform cursor, ref Vector2 velocityRef)
        {
            if (position == null)
            {
                cursor.gameObject.SetActive(false);
                return;
            }

            cursor.gameObject.SetActive(true);
            var newPos = Vector2.SmoothDamp(
                cursor.anchoredPosition, position.Value * scale,
                ref velocityRef, smoothTime, maxSpeed);
            cursor.anchoredPosition = newPos;
        }
    }
}
