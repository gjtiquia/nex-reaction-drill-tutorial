#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    // This component publish poses captured by MDK.
    // There are three flavors, raw, smoothed and homographical.
    // When this proxy is enabled, it will try to extract the latest body detection from MDKController, perform
    // smoothing (and homographical transform if enabled) and make the stable body detections available for other
    // components. The smoothing is done per frame so that they are more stable over time (instead of the more sporadic
    // body detection timing from MDK).
    // Even though Update() timing is non-deterministic between components, it is safe to ask for the body detections
    // from the BodyPoseController since we ensure that the smoothing and computation happens once and only once per
    // rendering frame.
    // The returned body pose is a SimplePose struct, with values of extended nodes like hands and centre of hip
    // that are not in the original detected pose. The wrapper struct contains the updated pixelsPerInch value for the
    // respective pose flavor. E.g. if the body pose value is smoothed, the SimplePose.pixelsPerInch is also
    // smoothed.
    public class BodyPoseController : MonoBehaviour
    {
        [SerializeField] private MdkController mdkController = null!;

        public MdkController MdkController
        {
            get => mdkController;
            set => mdkController = value;
        }

        [Header("Smooth configs")]
        [SerializeField] private TickDebouncedBoolean.DebounceConfig poseDebounceConfig = new(3, 6);

        [SerializeField] private TickDebouncedBoolean.DebounceConfig poseNodeDebounceConfig = new(2, 5);
        [SerializeField] private OneEuroFilterConfig nodeSmoothConfig = new(0.002f, 0.002f, 3f);

        [Header("Homographical Support (Experimental)")]
        [SerializeField] private bool supportHomographicalPoses;

        private int rawPoseRevision;

        public enum PoseFlavor
        {
            Raw = 0,
            Smoothed = 1,
            SmoothHomographical = 2,
            RawHomographical = 3,
        }

        private double rawFrameTime;

        private readonly List<PoseSmoothEngine> poseSmoothEngines = new();

        public SimplePose? GetBodyPose(int index, PoseFlavor flavor)
        {
            ProcessIfNeeded();
            if (index < 0 || index >= poseSmoothEngines.Count) return null;
            var smoothEngine = poseSmoothEngines[index];
            return flavor switch
            {
                PoseFlavor.Raw => smoothEngine.GetRawPose(),
                PoseFlavor.Smoothed => smoothEngine.GetSmoothedPose(),
                PoseFlavor.SmoothHomographical => smoothEngine.GetSmoothedHomographicalPose(),
                PoseFlavor.RawHomographical => smoothEngine.GetRawHomographicalPose(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public bool TryGetBodyPose(int index, PoseFlavor flavor, out SimplePose bodyPose)
        {
            var maybeBodyPose = GetBodyPose(index, flavor);

            if (maybeBodyPose == null)
            {
                bodyPose = new SimplePose();
                return false;
            }

            bodyPose = maybeBodyPose.Value;
            return true;
        }

        private void Awake()
        {
            if (mdkController == null)
            {
                mdkController = GetComponent<MdkController>();
            }

            if (mdkController != null)
            {
                mdkController.GetBodyPoseDetectionStream()
                    .Subscribe(HandleBodyPoseDetection, this.GetCancellationTokenOnDestroy());
            }
        }

        private int SyncSmoothEngineCount()
        {
            var n = mdkController.PlayerCount;
            // Remove extra engines and add new ones.
            while (n < poseSmoothEngines.Count)
            {
                poseSmoothEngines.RemoveAt(poseSmoothEngines.Count - 1);
            }

            while (poseSmoothEngines.Count < n)
            {
                poseSmoothEngines.Add(
                    new PoseSmoothEngine(poseDebounceConfig, poseNodeDebounceConfig, nodeSmoothConfig));
            }

            return n;
        }

        private void HandleBodyPoseDetection(BodyPoseDetection rawDetection)
        {
            ++rawPoseRevision;
            var n = SyncSmoothEngineCount();

            for (var playerIndex = 0; playerIndex < n; ++playerIndex)
            {
                var playerPose = rawDetection.GetPlayerPose(playerIndex);
                poseSmoothEngines[playerIndex].UpdateRawPose(playerPose?.bodyPose);
            }
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
            foreach (var engine in poseSmoothEngines)
            {
                engine.Update();
            }

            if (!supportHomographicalPoses) return;
            var (pitch, fov) = DewarpWrapper.GetDewarpInfo();
            // Assuming aspect normalized coordinate.
            var homographicalTransform = HomographicalTransform.Compute(pitch, fov, Constants.rawFrameAspectRatio, 1);
            foreach (var engine in poseSmoothEngines)
            {
                engine.UpdateHomography(homographicalTransform);
            }
        }

        private void Update()
        {
            ProcessIfNeeded();
        }

        public IUniTaskAsyncEnumerable<SimplePose> GetBodyPoseStream(int index, PoseFlavor flavor)
        {
            var lastRevision = rawPoseRevision;
            return UniTaskAsyncEnumerable.Create<SimplePose>(async (writer, token) =>
            {
                await UniTask.Yield();
                while (!token.IsCancellationRequested)
                {
                    // If subscriber is expecting the raw flavor
                    // Only push event when there is a new raw pose
                    // Do not push on every update
                    var shouldSkipRaw = flavor == PoseFlavor.Raw && rawPoseRevision == lastRevision;
                    lastRevision = rawPoseRevision;

                    if (!shouldSkipRaw && TryGetBodyPose(index, flavor, out var bodyPose))
                    {
                        await writer.YieldAsync(bodyPose);
                    }

                    await UniTask.Yield();
                }
            });
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var engine in poseSmoothEngines)
            {
                engine.SetOneEuroFilterConfig(nodeSmoothConfig);
            }
        }
#endif
    }
}
