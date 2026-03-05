#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlaygroundSDKEssentials.Examples.GrabbingBall
{
    public class Grabbable : UIBehaviour
    {
        [SerializeField] private Rigidbody2D rb2d = null!;
        [SerializeField] private Graphic graphic = null!;
        private bool isGrabbed;
        private bool isTouched;
        private Vector2 initialPosition;
        private float defaultGravityScale;
        private GrabCursor? grabbingCursor;
        private readonly Dictionary<GrabCursor, Action<bool>> cursorGrabHandlers = new();

        private Vector2 cursorOffset;
        private RectTransform rectTransform = null!;

        protected override void Start()
        {
            rectTransform = (RectTransform)transform;
            initialPosition = rectTransform.anchoredPosition;
            defaultGravityScale = rb2d.gravityScale;
            graphic.color = Color.yellow;
        }

        private void ResetStates()
        {
            rectTransform.anchoredPosition = initialPosition;
#if UNITY_6000_0_OR_NEWER
            rb2d.linearVelocity = Vector2.zero;
            rb2d.bodyType = RigidbodyType2D.Dynamic;
#else
            rb2d.velocity = Vector2.zero;
            rb2d.isKinematic = false;
#endif
            rb2d.angularVelocity = 0f;
            rb2d.gravityScale = defaultGravityScale;
            isGrabbed = false;
            isTouched = false;
            grabbingCursor = null;
            cursorGrabHandlers.Clear();
            graphic.color = Color.yellow;
        }

        private void LateUpdate()
        {
            if (isGrabbed && grabbingCursor is not null)
            {
                // Use anchoredPosition consistently for UI elements - updates after cursor position is set
                rectTransform.anchoredPosition = grabbingCursor.RectTransform.anchoredPosition + cursorOffset;
                graphic.color = new Color32(0x8C, 0x88, 0x00, 0xFF);
            }
            else if (isTouched)
            {
                graphic.color = Color.green;
            }
            else
            {
                graphic.color = Color.yellow;
            }

            // Reset position if falling below threshold
            if (rectTransform.anchoredPosition.y < -2000f)
            {
                ResetStates();
            }
        }

        protected override void OnDestroy()
        {
            foreach (var (cursor, handler) in cursorGrabHandlers)
            {
                cursor.OnGrabStateChanged -= handler;
            }

            cursorGrabHandlers.Clear();

            base.OnDestroy();
        }

        private void OnGrabbed(GrabCursor cursor)
        {
            // Set kinematic to prevent physics from interfering while grabbed
#if UNITY_6000_0_OR_NEWER
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.linearVelocity = Vector2.zero;
#else
            rb2d.isKinematic = true;
            rb2d.velocity = Vector2.zero;
#endif
            rb2d.angularVelocity = 0f;
            isGrabbed = true;
            grabbingCursor = cursor;

            // Calculate offset using anchoredPosition for consistent UI positioning
            cursorOffset = rectTransform.anchoredPosition - cursor.RectTransform.anchoredPosition;
        }

        private void OnReleased(GrabCursor cursor)
        {
            if (grabbingCursor != cursor) // Prevent it is being released by a cursor that is not grabbing it
            {
                return;
            }

            isGrabbed = false;
            // Re-enable physics simulation on release
#if UNITY_6000_0_OR_NEWER
            rb2d.bodyType = RigidbodyType2D.Dynamic;
#else
            rb2d.isKinematic = false;
#endif
            rb2d.gravityScale = defaultGravityScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<GrabCursor>(out var cursor)) return;

            isTouched = true;
            // Subscribe to grab state change
            Action<bool> handler = isGrabbing =>
            {
                if (isGrabbing)
                {
                    OnGrabbed(cursor);
                }
                else
                {
                    OnReleased(cursor);
                }
            };
            cursor.OnGrabStateChanged += handler;
            cursorGrabHandlers[cursor] = handler;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // if all cursors are out of the object, set IsTouched to false
            if (!other.TryGetComponent<GrabCursor>(out var cursor)) return;

            // If this cursor is currently grabbing the object, release it
            if (grabbingCursor == cursor)
            {
                OnReleased(cursor);
            }

            // Unsubscribe from grab state change
            if (!cursorGrabHandlers.TryGetValue(cursor, out var handler)) return;
            cursor.OnGrabStateChanged -= handler;
            cursorGrabHandlers.Remove(cursor);

            if (cursorGrabHandlers.Count == 0)
            {
                isTouched = false;
            }
        }
    }
}
