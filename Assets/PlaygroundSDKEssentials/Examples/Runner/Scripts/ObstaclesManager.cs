using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Nex.Essentials.Examples.Runner
{
    public class ObstaclesManager : MonoBehaviour
    {
        public enum LayoutModeType
        {
            Normal,
            LaneBased,
        }

        [SerializeField] private LayoutModeType layoutMode = LayoutModeType.LaneBased;
        [SerializeField] private List<ObstacleTileSettings> tiles = new();
        [SerializeField] private List<ObstacleTileSettings> laneTiles = new();

        public LayoutModeType LayoutMode => layoutMode;

        [Header("Obstacle Spawn Rate")]
        [FormerlySerializedAs("spawnInterval")] [SerializeField, InspectorName("Min Spawn Interval (ms)")]
        private int minSpawnInterval;

        [SerializeField, InspectorName("Max Spawn Interval (ms)")]
        private int maxSpawnInterval;

        [SerializeField] private float spawnRateAcceleration;

        [SerializeField] private bool isSpawning = true;

        [Header("Obstacle Speed")]
        [SerializeField] private float minSpeed;

        [SerializeField] private float maxSpeed;
        [SerializeField] private float speedAcceleration;

        [Header("Bombs")]
        [SerializeField] private float bombsProbabilityAcceleration;

        [SerializeField] private float bombSpeedMultiplier = 1f;

        private ObstacleTileSettings lastTile;
        public bool IsSpawning => isSpawning;

        private void Start()
        {
            Run(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void ToggleSpawning()
        {
            isSpawning = !isSpawning;
        }

        public void ToggleSpawning(bool value)
        {
            isSpawning = value;
        }

        public void SetLayoutMode(LayoutModeType value)
        {
            layoutMode = value;
        }

        private async UniTaskVoid Run(CancellationToken cancellationToken)
        {
            while (isActiveAndEnabled)
            {
                var timeSinceStart = Time.time - RunnerDriver.Instance.GameStartTime;
                var spawnObstacleTileDelay = Mathf.RoundToInt(Mathf.Lerp(maxSpawnInterval, minSpawnInterval,
                    timeSinceStart * spawnRateAcceleration));
                if (!isSpawning)
                {
                    lastTile = null;
                    await UniTask.Delay(spawnObstacleTileDelay, cancellationToken: cancellationToken);
                    continue;
                }

                var nextTile = lastTile;
                var useRandom = true;

                if (lastTile != null)
                {
                    var nextTileProbabilities = lastTile.nextTileProbabilities;
                    var randFloat = Random.value;
                    var tempCumulative = 0f;

                    foreach (var tileProbability in nextTileProbabilities)
                    {
                        tempCumulative += tileProbability.probability;
                        if (!(randFloat < tempCumulative)) continue;
                        useRandom = false;
                        nextTile = tileProbability.tileSettings;
                        break;
                    }
                }

                var tileList = layoutMode == LayoutModeType.LaneBased ? laneTiles : tiles;
                nextTile = useRandom ? tileList[Random.Range(0, tileList.Count)] : nextTile;

                // Same tile as last tile, regenerate
                if (nextTile.Equals(lastTile) && tileList.Count > 1) continue;

                lastTile = nextTile;

                var newSpeed = Mathf.Lerp(minSpeed, maxSpeed, timeSinceStart * speedAcceleration);

                for (var i = 0; i < nextTile.obstaclePositions.Length; i++)
                {
                    var prefab = nextTile.obstaclePrefabs[i % nextTile.obstaclePrefabs.Length];
                    var obstacle = Instantiate(prefab, transform);
                    obstacle.transform.localPosition += nextTile.obstaclePositions[i];
                    obstacle.SetSpeed(newSpeed);
                }

                var currentBombProbability = Mathf.Min(timeSinceStart * bombsProbabilityAcceleration, 0.75f);
                for (var i = 0; i < nextTile.bombsPositions.Length; i++)
                {
                    if (Random.value > currentBombProbability) continue;
                    var prefab = nextTile.bombsPrefabs[i % nextTile.bombsPrefabs.Length];
                    var bomb = Instantiate(prefab, transform);
                    var bombLocalPosition = nextTile.bombsPositions[i];
                    var distanceAdjusted = (transform.position.z + bombLocalPosition.z) * (bombSpeedMultiplier - 1);
                    bomb.transform.localPosition += bombLocalPosition + new Vector3(0, 0, distanceAdjusted);
                    bomb.SetSpeed(newSpeed * bombSpeedMultiplier);
                }

                await UniTask.Delay(spawnObstacleTileDelay, cancellationToken: cancellationToken);
            }
        }
    }
}
