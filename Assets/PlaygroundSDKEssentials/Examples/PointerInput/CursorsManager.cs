#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Nex.Essentials.Examples.PointerInput
{
    public class CursorsManager : MonoBehaviour
    {
        [SerializeField] private CursorProducer leftCursorProducer = null!;
        [SerializeField] private CursorProducer rightCursorProducer = null!;

        [SerializeField] private RectTransform leftCursor = null!;
        [SerializeField] private RectTransform rightCursor = null!;
        [SerializeField] private float scale = 1080;

        private void Start()
        {
            leftCursorProducer.CursorPositionStream.Subscribe(HandleLeftCursor, this.GetCancellationTokenOnDestroy());
            rightCursorProducer.CursorPositionStream.Subscribe(HandleRightCursor, this.GetCancellationTokenOnDestroy());
        }

        private void HandleLeftCursor(Vector2? position)
        {
            HandleCursor(position, leftCursor);
        }

        private void HandleRightCursor(Vector2? position)
        {
            HandleCursor(position, rightCursor);
        }

        private void HandleCursor(Vector2? position, RectTransform cursor)
        {
            if (position == null)
            {
                cursor.gameObject.SetActive(false);
                return;
            }

            cursor.gameObject.SetActive(true);
            cursor.anchoredPosition = position.Value * scale;
        }
    }
}
