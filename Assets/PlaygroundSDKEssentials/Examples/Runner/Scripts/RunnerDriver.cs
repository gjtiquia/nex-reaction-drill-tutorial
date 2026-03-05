#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Nex.Essentials.Examples.Runner
{
    public class RunnerDriver : MonoBehaviour
    {
        [Serializable]
        private class SetupConfig
        {
            public GameObject setupPanel = null!;
            public TMP_Text setupInstruction = null!;
            public OnePlayerSetupDetector setupPrefab = null!;
            public float setupPrefabYPosition = 432;
            public float canvasWidth = 1920;
        }

        [Serializable]
        private class RestartConfig
        {
            public int restartDelay = 8;

            // Start spawning obstacles before the game full resume
            public int obstacleSpawnDelay = 5;
        }

        [SerializeField] private MdkController mdkController = null!;
        [SerializeField] private PlayAreaController playAreaController = null!;
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private PlayAreaPreviewFrameProvider playAreaPreviewFrameProvider = null!;
        [SerializeField] private ObstaclesManager obstaclesManager = null!;
        [SerializeField] private GameObject playerBody = null!;
        [SerializeField] private SetupConfig setupConfig = null!;
        [SerializeField] private RestartConfig restartConfig = null!;

        [Header("UI")]
        [SerializeField] private TMP_Text timeLabel = null!;

        [SerializeField] private TMP_Text highestLabel = null!;

        [Header("Crashed Indicator")]
        [SerializeField] private GameObject crashedIndicator = null!;

        [SerializeField] private float crashedIndicatorTime;

        public float GameStartTime { get; private set; }
        private float lastCollisionTime;
        private float highestRecord;
        private bool needToRestartGame;
        private bool isGameRunning = true;

        public static RunnerDriver Instance { get; private set; } = null!;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            mdkController.StartRunning().Forget();

            Run(destroyCancellationToken).Forget();
        }

        [ContextMenu("Restart Game")]
        public void RestartGame()
        {
            needToRestartGame = true;
        }

        [ContextMenu("Toggle Spawning")]
        public bool ToggleSpawning()
        {
            if (!needToRestartGame) obstaclesManager.ToggleSpawning();
            isGameRunning = obstaclesManager.IsSpawning && !needToRestartGame;
            if (isGameRunning)
            {
                GameStartTime = Time.time;
                lastCollisionTime = GameStartTime;
            }

            return isGameRunning;
        }

        private async UniTask RunSetup(CancellationToken cancellationToken)
        {
            obstaclesManager.ToggleSpawning(false);
            setupConfig.setupPanel.SetActive(true);

            // For each player, we want to figure out if they are holding up one of their hands or not.
            // And which hand they are holding up.
            // If both are raised, default to right hand.
            const int playerCount = 1;
            var playerSetupDetectors = new OnePlayerSetupDetector[playerCount];
            for (var playerIndex = 0; playerIndex < playerCount; ++playerIndex)
            {
                var playerPosition = playAreaController.PlayerPositions[playerIndex];
                var framePosition = new Vector2(playerPosition * setupConfig.canvasWidth,
                    setupConfig.setupPrefabYPosition);

                // Instantiate the player setup detector prefab inside the setup panel
                var setupController = Instantiate(setupConfig.setupPrefab, setupConfig.setupPanel.transform, false);
                setupController.Initialize(playAreaController, bodyPoseController, playerIndex, framePosition,
                    playAreaPreviewFrameProvider);
                playerSetupDetectors[playerIndex] = setupController;
            }

            await UniTask.WhenAll(playerSetupDetectors.Select(detector =>
                detector.WaitUntilIsReady(cancellationToken)));

            setupConfig.setupPanel.SetActive(false);
        }

        private async UniTaskVoid Run(CancellationToken cancellationToken)
        {
            mdkController.DewarpLocked = false;
            mdkController.EnableConsistency = false;
            playAreaController.Locked = false;
            await RunSetup(cancellationToken);

            mdkController.DewarpLocked = true;
            mdkController.EnableConsistency = true;
            playAreaController.Locked = true;
            await RunGame(cancellationToken);
        }

        private async UniTask RunGame(CancellationToken cancellationToken)
        {
            obstaclesManager.ToggleSpawning(true);
            playerBody.SetActive(true);

            GameStartTime = Time.time;
            lastCollisionTime = GameStartTime;
            while (isActiveAndEnabled)
            {
                if (needToRestartGame && isGameRunning)
                {
                    await _RestartGame(cancellationToken);
                }

                var currentTime = isGameRunning ? Time.time - lastCollisionTime : 0;
                timeLabel.text = $"{currentTime:0.000}";
                crashedIndicator.SetActive(currentTime <= crashedIndicatorTime &&
                                           !Mathf.Approximately(lastCollisionTime, GameStartTime) && isGameRunning);

                if (currentTime > highestRecord)
                {
                    highestRecord = currentTime;
                    highestLabel.text = $"{highestRecord:0.000}";
                }

                await UniTask.Delay(1, cancellationToken: cancellationToken);
            }
        }

        private async UniTask _RestartGame(CancellationToken cancellationToken)
        {
            obstaclesManager.ToggleSpawning(false);
            crashedIndicator.SetActive(false);

            // Stop spawning obstacles to create a gap
            await UniTask.Delay(restartConfig.obstacleSpawnDelay * 1000, cancellationToken: cancellationToken);

            GameStartTime = Time.time;
            obstaclesManager.ToggleSpawning(true);

            // Wait for a bit more until the timer restarts
            var additionalDelay = (restartConfig.restartDelay - restartConfig.obstacleSpawnDelay) * 1000;
            await UniTask.Delay(additionalDelay, cancellationToken: cancellationToken);

            needToRestartGame = false;
            GameStartTime = Time.time;
            lastCollisionTime = GameStartTime;
        }

        public void OnCollidedWithObstacles()
        {
            if (!needToRestartGame)
            {
                lastCollisionTime = Time.time;
            }
        }

        public void OnCollidedWithBombs()
        {
            if (!needToRestartGame)
            {
                lastCollisionTime = Time.time;
            }
        }
    }
}
