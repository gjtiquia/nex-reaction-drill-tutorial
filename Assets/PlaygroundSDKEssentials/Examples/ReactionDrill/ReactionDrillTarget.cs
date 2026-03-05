#nullable enable

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nex.Essentials.Examples.ReactionDrill
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ReactionDrillTarget : MonoBehaviour
    {
        [SerializeField] private ReactionDrillGameConfigs configs = null!;
        [SerializeField] private TMP_Text scoreLabel = null!;

        // When something is hit, invoke with the score.
        private event Action<HandType, int>? OnHit;

        private SpriteRenderer spriteRenderer = null!;
        private string targetTag = null!;
        private HandType currentHandType;

        protected void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        class RandomPositionGenerator
        {
            private readonly List<Vector2> randomPool = new();
            private readonly Queue<Vector2> stalePool = new();

            private const int stalePoolSize = 4;

            public RandomPositionGenerator(RectInt grid)
            {
                var xMin = grid.xMin;
                var xMax = grid.xMax;
                var yMin = grid.yMin;
                var yMax = grid.yMax;
                for (var x = xMin; x <= xMax; ++x)
                {
                    for (var y = yMin; y <= yMax; ++y)
                    {
                        randomPool.Add(new Vector2(x, y));
                    }
                }
            }

            public Vector2 Generate()
            {
                // Pick something from the random location.
                var randomIndex = Random.Range(0, randomPool.Count);
                // Pop it.
                var picked = randomPool[randomIndex];
                randomPool[randomIndex] = randomPool[^1];
                randomPool.RemoveAt(randomPool.Count - 1);
                stalePool.Enqueue(picked);
                while (stalePool.Count > stalePoolSize)
                {
                    // Recycle old entries.
                    randomPool.Add(stalePool.Dequeue());
                }
                return picked;
            }
        }

        private static RandomPositionGenerator leftPositionGenerator = null!;
        private static RandomPositionGenerator rightPositionGenerator = null!;

        // These are the ones that should not appear in a while.
        public static void PrepareRandomness(RectInt targetRect)
        {
            // The leftRect should be flipped.
            var leftTargetRect = new RectInt(-targetRect.xMax, targetRect.yMin, targetRect.width, targetRect.height);
            leftPositionGenerator = new RandomPositionGenerator(leftTargetRect);
            rightPositionGenerator = new RandomPositionGenerator(targetRect);
        }

        public void Initialize(HandType handType, Action<HandType, int> onHit)
        {
            currentHandType = handType;
            // Find a point within playArea. We inset each direction by 10%.
            var generator = handType switch
            {
                HandType.Left => leftPositionGenerator,
                HandType.Right => rightPositionGenerator,
                _ => throw new ArgumentOutOfRangeException(nameof(handType), handType, null)
            };
            transform.localPosition = generator.Generate();
            spriteRenderer.color = configs.GetColor(handType);
            targetTag = configs.GetTag(handType);
            OnHit += onHit;
        }

        private float expiryTime;
        private int currentScore;
        private float scoreRatio;

        protected void Start()
        {
            expiryTime = Time.time + configs.targetLifeTime;
            scoreRatio = configs.targetMaxScore / configs.targetLifeTime;
            currentScore = configs.targetMaxScore;
        }

        private void Update()
        {
            var remaining = expiryTime - Time.time;
            if (remaining <= 0)
            {
                OnHit?.Invoke(currentHandType, 0);  // No hitting.
                Destroy(gameObject);
                return;
            }

            var score = Mathf.CeilToInt(remaining * scoreRatio);
            if (score == currentScore) return;
            currentScore = score;
            scoreLabel.text = $"{currentScore}";
        }

        private void OnCollisionEnter(Collision other)
        {
            // Check if the other has a matching tag.
            if (!other.gameObject.CompareTag(targetTag)) return;
            // Report it.
            OnHit?.Invoke(currentHandType, currentScore);
            // Hit, gone.
            Destroy(gameObject);
        }
    }
}
