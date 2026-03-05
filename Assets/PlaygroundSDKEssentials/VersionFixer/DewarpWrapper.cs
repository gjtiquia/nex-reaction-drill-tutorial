#nullable enable

using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    public static class DewarpWrapper
    {
        public static (float pitch, float fov) GetDewarpInfo()
        {
#if UNITY_EDITOR
            return (0, 67);  // 67 is just a shim for Mac Camera.
#elif MDK_1_X
            var info = CvDetectionManager.dewarpController.StableDewarpInfo;
            return (info.tilt, info.fov);
#elif MDK_2_UP
            var window = CvDetectionManager.dewarpAutoTiltController.StableDewarpWindow;
            return (window.pitch, window.fov);
#endif
        }

        public static bool GetDewarpLocked()
        {
            ContinuousAutoTiltMode currMode;
#if MDK_1_X
            currMode = CvDetectionManager.dewarpController.continuousAutoTiltMode;
#elif MDK_2_UP
            currMode = CvDetectionManager.dewarpAutoTiltController.continuousAutoTiltMode;
#else
            currMode = ContinuousAutoTiltMode.Off;
#endif
            return currMode == ContinuousAutoTiltMode.Off;
        }

        public static void SetDewarpLocked(bool value)
        {
            var targetMode = value ? ContinuousAutoTiltMode.Off : ContinuousAutoTiltMode.Recovery;
            // ReSharper disable once JoinDeclarationAndInitializer
            ContinuousAutoTiltMode currMode;
#if MDK_1_X
            currMode = CvDetectionManager.dewarpController.continuousAutoTiltMode;
#elif MDK_2_UP
            currMode = CvDetectionManager.dewarpAutoTiltController.continuousAutoTiltMode;
#else
            currMode = ContinuousAutoTiltMode.Off;
#endif
            if (currMode == targetMode) return;
#if MDK_1_X
            CvDetectionManager.dewarpController.continuousAutoTiltMode = targetMode;
#elif MDK_2_UP
            CvDetectionManager.dewarpAutoTiltController.continuousAutoTiltMode = targetMode;
#else
            // No op.
#endif
            Debug.Log($"Dewarp changed: {(value ? "Locked" : "Unlocked")}");
        }
    }
}
