using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nex.Essentials.Test
{
    public class RandomDot : MonoBehaviour
    {
        [SerializeField] private float maxX = 800;
        [SerializeField] private float maxY = 500;

        [SerializeField] private float smoothTime = 4;
        [SerializeField] private float maxSpeed = 500;
        [SerializeField] private float rotationSpeed = 180;

        private void Start()
        {
            Run(destroyCancellationToken).Forget();
        }

        private void Update()
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }

        private async UniTaskVoid Run(CancellationToken cancellationToken)
        {
            // Delay for 0 ~ 1000 ms.
            const int maxInitialDelayMs = 1000;
            const int minIntervalMs = 2000;
            const int maxIntervalMs = 6000;
            await UniTask.Delay(Random.Range(0, maxInitialDelayMs), cancellationToken: cancellationToken);
            var rectTransform = (RectTransform)transform;
            while (!cancellationToken.IsCancellationRequested)
            {
                var targetPos = new Vector2(Random.Range(-maxX, maxX), Random.Range(-maxY, maxY));
                var currentVelocity = Vector2.zero;
                await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate().WithCancellation(cancellationToken))
                {
                    var newPosition = rectTransform.anchoredPosition =
                        Vector2.SmoothDamp(rectTransform.anchoredPosition, targetPos, ref currentVelocity, smoothTime,
                            maxSpeed);
                    // Check if the position is close enough to the target position.
                    if ((newPosition - targetPos).sqrMagnitude < 4f) break;
                }
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Delay(Random.Range(minIntervalMs, maxIntervalMs), cancellationToken: cancellationToken);
            }
        }
    }
}
