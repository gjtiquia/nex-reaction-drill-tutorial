using System;
using TMPro;
using UnityEngine;

namespace Nex.Essentials.Examples.Runner
{
    public class RunnerDebugManager : MonoBehaviour
    {
        private enum Key
        {
            Up = 0,
            Right = 1,
            Down = 2,
            Left = 3,
            Back = 4,
            Enter = 5,
        }

        [SerializeField] private TMP_Text label;
        private string modeString;
        private string layoutModeString;
        private string obstacleString = "Obstacles enabled";
        private string shadowString = "Shadow disabled";
        private string leaningString = "Leaning disabled";
        private string xScaleString = "X-Sensitivity: 0.45";

        [SerializeField] private Key[] modeKeySequence = null!;
        [SerializeField] private Key[] obstacleKeySequence = null!;
        [SerializeField] private Key[] layoutModeKeySequence = null!;
        [SerializeField] private Key[] shadowKeySequence = null!;
        [SerializeField] private Key[] enableLeaningSequence = null!;
        [SerializeField] private Key[] xScaleSequence = null!;

        private int[] modePrefixLength = null!;
        private int[] obstaclePrefixLength = null!;
        private int[] layoutModePrefixLength = null!;
        private int[] shadowPrefixLength = null!;
        private int[] enableLeaningPrefixLength = null!;
        private int[] xScalePrefixLength = null!;

        private int modeKeyMatchedLength;
        private int obstacleKeyMatchedLength;
        private int layoutModeKeyMatchedLength;
        private int shadowKeyMatchedLength;
        private int enableLeaningKeyMatchedLength;
        private int xScaleKeyMatchedLength;

        [SerializeField] private PlayerBody playerBody = null!;
        [SerializeField] private ObstaclesManager obstaclesManager = null!;
        [SerializeField] private PlayerBody shadowPlayer = null!;

        [Serializable]
        public class ModesObjects
        {
            public GameObject[] absoluteObjects;
            public GameObject[] leanObjects;
            public GameObject[] absoluteAndSnapObjects;
        }

        [Tooltip("Objects to enable/disable when mode changes")]
        public ModesObjects modesObjects;

        private void Awake()
        {
            // Compute the prefix length array.
            modePrefixLength = InitializePrefixLength(modeKeySequence);
            obstaclePrefixLength = InitializePrefixLength(obstacleKeySequence);
            layoutModePrefixLength = InitializePrefixLength(layoutModeKeySequence);
            shadowPrefixLength = InitializePrefixLength(shadowKeySequence);
            enableLeaningPrefixLength = InitializePrefixLength(enableLeaningSequence);
            xScalePrefixLength = InitializePrefixLength(xScaleSequence);
        }

        private static int[] InitializePrefixLength(Key[] keySequence)
        {
            var n = keySequence.Length;
            var prefixLength = new int[n];
            prefixLength[0] = 0;
            for (var i = 1; i < n; ++i)
            {
                prefixLength[i] = prefixLength[i - 1];
                while (keySequence[prefixLength[i]] != keySequence[i] && prefixLength[i] > 0)
                {
                    prefixLength[i] = prefixLength[prefixLength[i] - 1];
                }

                if (keySequence[prefixLength[i]] == keySequence[i]) ++prefixLength[i];
            }

            return prefixLength;
        }

        private static Key? GetPressedKey()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) return Key.Up;
            if (Input.GetKeyDown(KeyCode.RightArrow)) return Key.Right;
            if (Input.GetKeyDown(KeyCode.DownArrow)) return Key.Down;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) return Key.Left;
            if (Input.GetKeyDown(KeyCode.Escape)) return Key.Back;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.JoystickButton0))
                return Key.Enter;
            return null;
        }

        private enum RunnerMode
        {
            Absolute,
            Lean,
            AbsoluteSnap,
        }

        private readonly RunnerMode[] runnerModes = { RunnerMode.AbsoluteSnap, RunnerMode.Absolute, RunnerMode.Lean };
        private int runnerModeIndex;

        private ObstaclesManager.LayoutModeType layoutMode;
        private bool isShadowEnabled;
        private bool isLeaningEnabled;

        private static float[] predefinedXScales = new float[]
        {
            0.5f,
            0.45f,
            0.4f,
            0.35f,
            0.3f,
        };

        private int xScaleIndex = 1;  // By default, it is 0.45f.

        private void Start()
        {
            ChangeRunnerMode(RunnerMode.AbsoluteSnap);

            // Get default layout mode
            layoutMode = obstaclesManager.LayoutMode;
            layoutModeString = obstaclesManager.LayoutMode switch
            {
                ObstaclesManager.LayoutModeType.Normal => "Normal",
                ObstaclesManager.LayoutModeType.LaneBased => "Lane Based",
                _ => throw new ArgumentOutOfRangeException()
            };
            UpdateDebugText();
        }

        private void Update()
        {
            // See if there is any key press.
            var optionalKey = GetPressedKey();
            if (!optionalKey.HasValue) return;
            var key = optionalKey.Value;

            HandleModeKey(key);
            HandleObstacleKey(key);
            HandleLayoutModeKey(key);
            HandleShadowKey(key);
            HandleEnableLeaningKey(key);
            HandleXScaleKey(key);
        }

        private void HandleModeKey(Key key)
        {
            while (modeKeyMatchedLength > 0 && modeKeySequence[modeKeyMatchedLength] != key)
            {
                modeKeyMatchedLength = modePrefixLength[modeKeyMatchedLength - 1];
            }

            if (modeKeySequence[modeKeyMatchedLength] == key) ++modeKeyMatchedLength;

            if (modeKeyMatchedLength != modeKeySequence.Length) return;

            // Mode key triggered
            modeKeyMatchedLength = 0;
            RunnerDriver.Instance.RestartGame();

            runnerModeIndex = (runnerModeIndex + 1) % runnerModes.Length;
            ChangeRunnerMode(runnerModes[runnerModeIndex]);
        }

        private void HandleObstacleKey(Key key)
        {
            while (obstacleKeyMatchedLength > 0 && obstacleKeySequence[obstacleKeyMatchedLength] != key)
            {
                obstacleKeyMatchedLength = obstaclePrefixLength[obstacleKeyMatchedLength - 1];
            }

            if (obstacleKeySequence[obstacleKeyMatchedLength] == key) ++obstacleKeyMatchedLength;

            if (obstacleKeyMatchedLength != obstacleKeySequence.Length) return;

            // Obstacle key triggered
            obstacleKeyMatchedLength = 0;
            var isSpawning = RunnerDriver.Instance.ToggleSpawning();

            obstacleString = isSpawning ? "Obstacles enabled" : "Obstacles disabled";
            UpdateDebugText();
        }

        private void HandleLayoutModeKey(Key key)
        {
            while (layoutModeKeyMatchedLength > 0 && layoutModeKeySequence[layoutModeKeyMatchedLength] != key)
            {
                layoutModeKeyMatchedLength = layoutModePrefixLength[layoutModeKeyMatchedLength - 1];
            }

            if (layoutModeKeySequence[layoutModeKeyMatchedLength] == key) ++layoutModeKeyMatchedLength;

            if (layoutModeKeyMatchedLength != layoutModeKeySequence.Length) return;

            // Mode key triggered
            layoutModeKeyMatchedLength = 0;
            RunnerDriver.Instance.RestartGame();

            ToggleLayoutMode();
            UpdateDebugText();
        }

        private void HandleShadowKey(Key key)
        {
            while (shadowKeyMatchedLength > 0 && shadowKeySequence[shadowKeyMatchedLength] != key)
            {
                shadowKeyMatchedLength = shadowPrefixLength[shadowKeyMatchedLength - 1];
            }

            if (shadowKeySequence[shadowKeyMatchedLength] == key) ++shadowKeyMatchedLength;

            if (shadowKeyMatchedLength != shadowKeySequence.Length) return;

            // Mode key triggered
            shadowKeyMatchedLength = 0;
            isShadowEnabled = !isShadowEnabled;
            shadowPlayer.gameObject.SetActive(isShadowEnabled);
            shadowString = isShadowEnabled ? "Shadow enabled" : "Shadow disabled";
            UpdateDebugText();
        }

        private void HandleEnableLeaningKey(Key key)
        {
            while (enableLeaningKeyMatchedLength > 0 && enableLeaningSequence[enableLeaningKeyMatchedLength] != key)
            {
                enableLeaningKeyMatchedLength = enableLeaningPrefixLength[enableLeaningKeyMatchedLength - 1];
            }

            if (enableLeaningSequence[enableLeaningKeyMatchedLength] == key) ++enableLeaningKeyMatchedLength;

            if (enableLeaningKeyMatchedLength != enableLeaningSequence.Length) return;

            // Leaning key triggered
            enableLeaningKeyMatchedLength = 0;

            isLeaningEnabled = !isLeaningEnabled;
            shadowPlayer.IsLeaningEnabled = isLeaningEnabled;
            playerBody.IsLeaningEnabled = isLeaningEnabled;
            leaningString = isLeaningEnabled ? "Leaning enabled" : "Leaning disabled";
            UpdateDebugText();
        }

        private void HandleXScaleKey(Key key)
        {
            while (xScaleKeyMatchedLength > 0 && xScaleSequence[xScaleKeyMatchedLength] != key)
            {
                xScaleKeyMatchedLength = xScalePrefixLength[xScaleKeyMatchedLength - 1];
            }
            if (xScaleSequence[xScaleKeyMatchedLength] == key) ++xScaleKeyMatchedLength;
            if (xScaleKeyMatchedLength != xScaleSequence.Length) return;
            // X Scale key triggered

            xScaleKeyMatchedLength = 0;
            xScaleIndex = (xScaleIndex + 1) % predefinedXScales.Length;
            var xScale = predefinedXScales[xScaleIndex];
            playerBody.SetAbsoluteXScale(xScale);
            shadowPlayer.SetAbsoluteXScale(xScale);
            xScaleString = $"X-Sensitivity: {xScale:0.00}";
            UpdateDebugText();
        }

        private void ChangeRunnerMode(RunnerMode runnerMode)
        {
            switch (runnerMode)
            {
                case RunnerMode.Absolute:
                    playerBody.SetXMode(PlayerBody.HorizontalMovementMode.Absolute);
                    modeString = "Absolute";
                    EnableObjects(modesObjects.absoluteObjects);
                    EnableObjects(modesObjects.leanObjects, false);
                    EnableObjects(modesObjects.absoluteAndSnapObjects, false);
                    break;
                case RunnerMode.Lean:
                    playerBody.SetXMode(PlayerBody.HorizontalMovementMode.Lean);
                    modeString = "Lean";
                    EnableObjects(modesObjects.absoluteObjects, false);
                    EnableObjects(modesObjects.leanObjects);
                    EnableObjects(modesObjects.absoluteAndSnapObjects, false);
                    break;
                case RunnerMode.AbsoluteSnap:
                    playerBody.SetXMode(PlayerBody.HorizontalMovementMode.Absolute, true);
                    modeString = "Absolute & Snap";
                    EnableObjects(modesObjects.absoluteObjects, false);
                    EnableObjects(modesObjects.leanObjects, false);
                    EnableObjects(modesObjects.absoluteAndSnapObjects);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runnerMode), runnerMode, null);
            }

            UpdateDebugText();
        }

        private void ToggleLayoutMode()
        {
            layoutMode = layoutMode == ObstaclesManager.LayoutModeType.Normal
                ? ObstaclesManager.LayoutModeType.LaneBased
                : ObstaclesManager.LayoutModeType.Normal;
            switch (layoutMode)
            {
                case ObstaclesManager.LayoutModeType.Normal:
                    obstaclesManager.SetLayoutMode(ObstaclesManager.LayoutModeType.Normal);
                    layoutModeString = "Original";
                    break;
                case ObstaclesManager.LayoutModeType.LaneBased:
                    obstaclesManager.SetLayoutMode(ObstaclesManager.LayoutModeType.LaneBased);
                    layoutModeString = "Lane Based";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layoutMode), layoutMode, null);
            }
        }

        private void UpdateDebugText()
        {
            label.text = $@"
Runner Mode: {modeString} (LLRR)
Obstacles Layout: {layoutModeString} (LLUU)
{obstacleString} (LLDDR)
{shadowString} (LLDDD)
{leaningString} (LLDDU)
{xScaleString} (LLDUU)";
        }

        private static void EnableObjects(GameObject[] gameObjects, bool enable = true)
        {
            foreach (var o in gameObjects)
            {
                o.SetActive(enable);
            }
        }
    }
}
