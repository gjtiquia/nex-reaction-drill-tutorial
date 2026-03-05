#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Nex.Platform;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nex.Essentials.Examples.ReactionDrill
{
    public class ReactionDrillGameDriver : MonoBehaviour
    {
        [SerializeField] private MdkController mdkController = null!;
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private PlayAreaController playAreaController = null!;
        [SerializeField] private ReactionDrillGameConfigs configs = null!;

        [Serializable]
        private class HandRaisedConfig
        {
            public TimeDebouncedBoolean.DebounceConfig debounceConfig = new(0.2f, 0.1f);
            public float fillProgressSpeed = 1f;
            public float dropProgressSpeed = 1f;
            public RectTransform progressBar = null!;
        }

        [Serializable]
        private class SetupConfig : HandRaisedConfig
        {
            public GameObject setupPanel = null!;
        }

        [SerializeField] private SetupConfig setupConfig = null!;

        [Serializable]
        private class RetryConfig : HandRaisedConfig
        {
            public int retryDelay = 3;
            public GameObject panel = null!;
            public TMP_Text retryLabel = null!;
        }

        [SerializeField] private RetryConfig retryConfig = null!;

        [SerializeField] private Transform stage = null!;
        [SerializeField] private GameObject gameUI = null!;
        [SerializeField] private TMP_Text scoreLabel = null!;
        [SerializeField] private TMP_Text highestScoreLabel = null!;
        [SerializeField] private TMP_Text timerLabel = null!;

        // Game play related.
        [SerializeField] private ReactionDrillTarget targetPrefab = null!;

        // ReSharper disable once NotAccessedField.Local
        private Rect playArea;

        private void Start()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            playAreaController.GetPlayAreaStream().Subscribe(UpdatePlayArea, cancellationToken);
            Run(cancellationToken).Forget();
        }

        private async UniTaskVoid Run(CancellationToken cancellationToken)
        {
            mdkController.DewarpLocked = false;
            mdkController.EnableConsistency = false;
            playAreaController.Locked = false;
            mdkController.SetPlayerPositions(new Vector2[] { new(0.5f, 0.5f) });
            await mdkController.StartRunning();

            GetHighestScore().Forget();

            await RunSetup(cancellationToken);
            mdkController.DewarpLocked = true;
            mdkController.EnableConsistency = true;
            playAreaController.Locked = true;

            while (cancellationToken.IsCancellationRequested == false)
            {
                await RunGame(cancellationToken);
                await RetryGame(cancellationToken);
            }
        }

        private async UniTask GetHighestScore()
        {
            var returnedHighestScore = await ReactionDrillGameHubWrapper.QueryHighestScore();
            if (returnedHighestScore != null)
            {
                highestScore = returnedHighestScore.Value;
                SetHighestScore();
            }
        }

        private void UpdatePlayArea(Rect newPlayArea)
        {
            playArea = newPlayArea;
        }

        private async UniTask RunSetup(CancellationToken cancellationToken)
        {
            // Now we enter setup state.
            setupConfig.setupPanel.SetActive(true);

            await WaitForAllHandRaised(setupConfig, cancellationToken);

            setupConfig.setupPanel.SetActive(false);
        }

        private async UniTask WaitForAllHandRaised(HandRaisedConfig handRaisedConfig,
            CancellationToken cancellationToken)
        {
            // For each player, we want to figure out if they are holding up one of their hands or not.
            var playerCount = mdkController.PlayerCount;
            var playerSetupStates = new TimeDebouncedBoolean[playerCount];
            for (var playerIndex = 0; playerIndex < playerCount; ++playerIndex)
            {
                playerSetupStates[playerIndex] = new TimeDebouncedBoolean(handRaisedConfig.debounceConfig);
            }

            var progress = 0f;

            await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var allReady = true;
                for (var playerIndex = 0; playerIndex < playerCount; ++playerIndex)
                {
                    var bodyPose = bodyPoseController.GetBodyPose(playerIndex, BodyPoseController.PoseFlavor.Smoothed);
                    var handRaised = bodyPose != null &&
                                     GestureUtils.IsAnyHandRaised(bodyPose.Value, playerSetupStates[playerIndex].Value);
                    if (!playerSetupStates[playerIndex].Update(handRaised))
                    {
                        allReady = false;
                    }
                }

                // Fill up if all ready, else drop.
                if (allReady)
                {
                    progress += Time.deltaTime * handRaisedConfig.fillProgressSpeed;
                }
                else
                {
                    progress = Mathf.Max(0f, progress - Time.deltaTime * handRaisedConfig.dropProgressSpeed);
                }

                handRaisedConfig.progressBar.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                if (progress >= 1) break;
            }
        }

        private int score;
        private int highestScore;
        private bool isPlaying;
        private GameObject targetsContainer = null!;

        private async UniTask RunGame(CancellationToken cancellationToken)
        {
            // Now run the game.
            // Analytics start play session
            GameAnalytics.Instance.SendStartPlaySessionEvent("ReactionDrill", 1);

            ReactionDrillTarget.PrepareRandomness(configs.targetRect);
            stage.gameObject.SetActive(true);
            gameUI.SetActive(true);
            var endTime = Time.time + configs.gameDuration;
            score = 0;
            isPlaying = true;

            // Contains all targets to destroy all at once when game ends
            targetsContainer = new GameObject("Targets");
            targetsContainer.transform.SetParent(stage);

            // Generate two targets.
            OnTargetHit(HandType.Left, 0);
            OnTargetHit(HandType.Right, 0);
            await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate().WithCancellation(cancellationToken))
            {
                var remaining = endTime - Time.time;
                if (remaining <= 0) break;
                // Count seconds.
                timerLabel.text = $"{remaining:00.000}";
            }

            isPlaying = false;

            timerLabel.text = "00.000";
            Destroy(targetsContainer);

            // Analytics ends play session
            GameAnalytics.Instance.SendStopPlaySessionEvent(
                new GameAnalyticsProperties { ["reason"] = "SessionTimeout", ["score"] = score, });

            // GameHub metrics
            ReactionDrillGameHubWrapper.SubmitScore(score).Forget();

            if (score > highestScore)
            {
                highestScore = score;
                SetHighestScore();
            }

            stage.gameObject.SetActive(false);
        }

        private void SetHighestScore()
        {
            highestScoreLabel.text = highestScore.ToString();
        }

        private void OnTargetHit(HandType handType, int delta)
        {
            if (!isPlaying) return;
            score += delta;
            scoreLabel.text = $"{score}";
            GenerateTarget(handType).Forget();
        }

        private async UniTaskVoid GenerateTarget(HandType handType)
        {
            var delay = Random.Range(configs.targetCooldown[0], configs.targetCooldown[1]);
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
            if (!isPlaying) return;
            var newTarget = Instantiate(targetPrefab, targetsContainer.transform);
            newTarget.Initialize(handType, OnTargetHit);
        }

        private async UniTask RetryGame(CancellationToken cancellationToken)
        {
            // Now we enter retry state.
            retryConfig.progressBar.anchorMax = new Vector2(0, 1);
            retryConfig.retryLabel.text = $"Final Score: {score}";
            retryConfig.panel.SetActive(true);

            // Display the score
            await UniTask.Delay(retryConfig.retryDelay * 1000, cancellationToken: destroyCancellationToken);
            retryConfig.retryLabel.text = "Raise your hand to retry!";

            await WaitForAllHandRaised(retryConfig, cancellationToken);

            retryConfig.panel.SetActive(false);
        }
    }
}
