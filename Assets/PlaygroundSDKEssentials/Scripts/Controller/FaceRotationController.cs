using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nex.Essentials
{
    public class FaceRotationController : MonoBehaviour
    {
        [SerializeField] private MdkController mdkController;
        [SerializeField] private FaceLandmarkWrapper faceLandmarkWrapper;

        [SerializeField, Tooltip("The smoothing configuration for the face rotation")]
        private OneEuroFilterConfig smoothingConfig;

        [SerializeField, Tooltip("The debounce configuration for whether the face is detected")]
        private TickDebouncedBoolean.DebounceConfig debounceConfig = new(3, 6);

        // The latest head pose rotations captured from the face landmark wrapper
        private List<Quaternion?> latestRotations = new();

        // The smooth engines for each player's face rotation
        private readonly List<RotationSmoothEngine> smoothEngines = new();

        public enum Flavor
        {
            Raw = 0,
            Smoothed = 1,
        }

        [Serializable]
        public class FaceRotationControllerOption
        {
            [Tooltip("Whether to disable mirror for the face rotation. If true, the face rotation will face forward.")]
            public bool disableMirror = true;

            [Tooltip("The flavor of the face rotation")]
            public Flavor flavor = Flavor.Raw;
        }

        private void OnEnable()
        {
            faceLandmarkWrapper.captureHeadPoseRotations += HandleHeadPoseRotation;
        }

        private void OnDisable()
        {
            faceLandmarkWrapper.captureHeadPoseRotations -= HandleHeadPoseRotation;
        }

        private int SyncSmoothEngineCount()
        {
            var n = mdkController.PlayerCount;
            // Remove extra engines and add new ones.
            while (n < smoothEngines.Count)
            {
                smoothEngines.RemoveAt(smoothEngines.Count - 1);
            }

            while (smoothEngines.Count < n)
            {
                smoothEngines.Add(new RotationSmoothEngine(debounceConfig, smoothingConfig));
            }

            return n;
        }

        private int processFrameCount = -1;

        private void ProcessIfNeeded()
        {
            if (Time.frameCount == processFrameCount) return;
            processFrameCount = Time.frameCount;
            Process();
        }

        private void Process()
        {
            for (var i = 0; i < smoothEngines.Count; ++i)
            {
                if (i >= latestRotations.Count) break;
                var rotation = latestRotations[i];
                var engine = smoothEngines[i];
                engine.Update(rotation, Time.unscaledDeltaTime);
            }
        }

        private void Update()
        {
            ProcessIfNeeded();
        }

        public Quaternion? GetLatestRotation(int index, FaceRotationControllerOption option)
        {
            ProcessIfNeeded();

            // Check if out of bound
            if (index < 0 || index >= smoothEngines.Count) return null;

            // Get the smooth engine
            var smoothEngine = smoothEngines[index];
            if (!smoothEngine.IsDetected) return null;

            return (option.flavor, option.disableMirror) switch
            {
                (Flavor.Raw, false) => smoothEngine.LatestRawValue,
                (Flavor.Raw, true) => MirrorQuaternion(smoothEngine.LatestRawValue),
                (Flavor.Smoothed, false) => smoothEngine.LatestSmoothedValue,
                (Flavor.Smoothed, true) => MirrorQuaternion(smoothEngine.LatestSmoothedValue),
                _ => null
            };
        }

        private static Quaternion? MirrorQuaternion(Quaternion? q)
        {
            if (!q.HasValue) return null;

            // Convert rotation to vectors
            var forward = q.Value * Vector3.forward;
            var up = q.Value * Vector3.up;

            // Mirror the vectors
            forward.z = -forward.z;
            up.z = -up.z;

            return Quaternion.LookRotation(forward, up);
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            smoothEngines.ForEach(engine =>
            {
                engine.Config = smoothingConfig;
                engine.DebounceConfig = debounceConfig;
            });
        }
#endif

        private void HandleHeadPoseRotation(List<Quaternion?> rotations)
        {
            SyncSmoothEngineCount();
            latestRotations = rotations;
        }
    }
}
