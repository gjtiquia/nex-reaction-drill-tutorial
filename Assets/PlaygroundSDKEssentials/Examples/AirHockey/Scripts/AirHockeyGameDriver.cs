#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nex.Essentials.Examples.AirHockey
{
    public class AirHockeyGameDriver : MonoBehaviour
    {
        [SerializeField] private MdkController mdkController = null!;
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private PlayAreaController playAreaController = null!;
        [SerializeField] private PlayAreaPreviewFrameProvider playAreaPreviewFrameProvider = null!;

        [Serializable]
        private class MenuScreenConfig
        {
            public GameObject menuPanel = null!;
            public Button singlePlayerButton = null!;
            public Button twoPlayerButton = null!;
        }

        [SerializeField] private MenuScreenConfig menuScreenConfig = null!;

        [Serializable]
        private class PlayerConfig
        {
            public float[] singlePlayerPosition = { 0.5f };
            public float[] twoPlayerPositions = { 0.3f, 0.6f };
        }

        [SerializeField] private PlayerConfig playerPositionsConfig = null!;

        [Serializable]
        private class SetupConfig
        {
            public GameObject setupPanel = null!;
            public TMP_Text setupInstruction = null!;
            public OnePlayerSetupDetector setupPrefab = null!;
            public float setupPrefabYPosition = 432;
            public float canvasWidth = 1920;
        }

        [SerializeField] private SetupConfig setupConfig = null!;

        [Serializable]
        private class HockeyKnobConfig
        {
            public KnobController knobOne = null!;
            public KnobController knobTwo = null!;

            public float knobZBoundaryMin;
            public float knobZBoundaryMax = 40;
            public float knobZOffsetMagnitude = 33;
        }

        [SerializeField] private HockeyKnobConfig hockeyKnobConfig = null!;

        [Serializable]
        private class InGameConfig
        {
            public float ppiMargin = 0.1f;

            public GameObject gameUI = null!;
            public GameObject gameEnvironment = null!;
            public TMP_Text player0ScoreLabel = null!;
            public TMP_Text player1ScoreLabel = null!;
            public GameObject player1Pip = null!;
            public GameObject puckPrefab = null!;
        }

        [SerializeField] private InGameConfig inGameConfig = null!;

        private int playerCount;
        private readonly int[] playerScores = { 0, 0 };
        private GameObject currentPuck = null!;

        private void Start()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            Run(cancellationToken).Forget();
        }

        private async UniTaskVoid Run(CancellationToken cancellationToken)
        {
            mdkController.DewarpLocked = false;
            mdkController.EnableConsistency = false;
            playAreaController.Locked = false;
            await mdkController.StartRunning();

            await RunMenu(cancellationToken);
            await RunSetup(cancellationToken);
            mdkController.DewarpLocked = true;
            mdkController.EnableConsistency = true;
            playAreaController.Locked = true;

            RunGame();
        }

        /// Choose the number of players and update the PlayAreaController
        private async UniTask RunMenu(CancellationToken cancellationToken)
        {
            menuScreenConfig.menuPanel.SetActive(true);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var onePlayerButton = menuScreenConfig.singlePlayerButton.OnClickAsync(linkedCts.Token);
            var onePlayerKey = WaitForKeyPress(KeyCode.LeftArrow, linkedCts.Token);
            var twoPlayerButton = menuScreenConfig.twoPlayerButton.OnClickAsync(linkedCts.Token);
            var twoPlayerKey = WaitForKeyPress(KeyCode.RightArrow, linkedCts.Token);

            var task = await UniTask.WhenAny(onePlayerButton, onePlayerKey, twoPlayerButton, twoPlayerKey);

            // One task finished, cancel others
            linkedCts.Cancel();

            // Set the number of players based on the button/key input
            switch (task)
            {
                case 0:
                case 1:
                    playerCount = 1;
                    playAreaController.PlayerPositions = playerPositionsConfig.singlePlayerPosition;
                    break;

                case 2:
                case 3:
                    playerCount = 2;
                    playAreaController.PlayerPositions = playerPositionsConfig.twoPlayerPositions;
                    break;
            }

            menuScreenConfig.menuPanel.SetActive(false);
        }

        private static async UniTask WaitForKeyPress(KeyCode keyCode, CancellationToken cancellationToken)
        {
            while (!Input.GetKeyDown(keyCode))
            {
                await UniTask.Yield(cancellationToken);
            }
        }

        private async UniTask RunSetup(CancellationToken cancellationToken)
        {
            // Now we enter setup state.
            // Switch the instructions depending on the number of players
            setupConfig.setupInstruction.text = playerCount switch
            {
                1 => "Stand in the middle and raise your hand to start.",
                2 => "Stand side by side and raise your aiming hand to start.",
                _ => setupConfig.setupInstruction.text
            };

            setupConfig.setupPanel.SetActive(true);

            // For each player, we want to figure out if they are holding up one of their hands or not.
            // And which hand they are holding up.
            // If both are raised, default to right hand.
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

            var playerAimingHands =
                await UniTask.WhenAll(playerSetupDetectors.Select(detector =>
                    detector.WaitUntilIsReady(cancellationToken)));

            switch (playerCount)
            {
                case 1:
                    StartKnobsForSinglePlayer();
                    inGameConfig.player1Pip.SetActive(false);
                    break;
                case 2:
                    StartKnobsForTwoPlayers(playerAimingHands[0], playerAimingHands[1]);
                    break;
            }

            setupConfig.setupPanel.SetActive(false);
        }

        private void StartKnobsForSinglePlayer()
        {
            // Set player 1 knob to left hand
            hockeyKnobConfig.knobOne.StartKnobTracking(0, SimplePose.NodeIndex.LeftHand,
                new Vector3(0, 0, -hockeyKnobConfig.knobZOffsetMagnitude), hockeyKnobConfig.knobZBoundaryMin,
                hockeyKnobConfig.knobZBoundaryMax);

            // Set player 2 knob to right hand
            hockeyKnobConfig.knobTwo.StartKnobTracking(0, SimplePose.NodeIndex.RightHand,
                new Vector3(0, 0, hockeyKnobConfig.knobZOffsetMagnitude), -hockeyKnobConfig.knobZBoundaryMax,
                -hockeyKnobConfig.knobZBoundaryMin);
        }

        private void StartKnobsForTwoPlayers(SimplePose.NodeIndex player0Hand, SimplePose.NodeIndex player1Hand)
        {
            // Set player 1 knob to pose 0 selected hand
            hockeyKnobConfig.knobOne.StartKnobTracking(0, player0Hand,
                new Vector3(0, 0, -hockeyKnobConfig.knobZOffsetMagnitude), hockeyKnobConfig.knobZBoundaryMin,
                hockeyKnobConfig.knobZBoundaryMax);

            // Set player 2 knob to pose 1 selected hand
            hockeyKnobConfig.knobTwo.StartKnobTracking(1, player1Hand,
                new Vector3(0, 0, hockeyKnobConfig.knobZOffsetMagnitude), -hockeyKnobConfig.knobZBoundaryMax,
                -hockeyKnobConfig.knobZBoundaryMin);
        }

        private void RunGame()
        {
            inGameConfig.gameUI.SetActive(true);
            inGameConfig.gameEnvironment.SetActive(true);
            SpawnPuck();
        }

        private void SpawnPuck()
        {
            currentPuck = Instantiate(inGameConfig.puckPrefab, Vector3.zero, Quaternion.identity);
        }

        private void UpdateScoreLabels()
        {
            inGameConfig.player0ScoreLabel.text = playerScores[0].ToString();
            inGameConfig.player1ScoreLabel.text = playerScores[1].ToString();
        }

        public async UniTask OnGoalScore(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerScores.Length) return;

            playerScores[playerIndex] += 1;
            UpdateScoreLabels();

            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
            Destroy(currentPuck);
            SpawnPuck();
        }
    }
}
