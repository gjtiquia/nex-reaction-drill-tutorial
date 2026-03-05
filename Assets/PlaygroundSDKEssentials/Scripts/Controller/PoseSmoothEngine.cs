#nullable enable

using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    // Poses from MDK are not always very stable.
    // As a result, it is important that we provide some sort of smoothing, especially when we need to render the poses.
    // The class is responsible for handling smoothing of poses. It also provides debouncing of pose results.
    // If a pose disappears for a frame or two, it will persist the last detected result. On the other hand, even when
    // a pose pops into view, it will wait for a bit before committing that pose.
    public sealed class PoseSmoothEngine
    {
        private TickDebouncedBoolean isDetected;

        // The last raw pose that is still detected.
        private BodyPose? lastDetectedRawPose;
        private BodyPose? smoothedBodyPose;
        private BodyPose? smoothedHomographicalBodyPose;
        private BodyPose? rawHomographicalBodyPose;

        private SimplePose? latestRawPoseWrapper;
        private SimplePose? latestSmoothedPoseWrapper;
        private SimplePose? latestSmoothedHomographicalPoseWrapper;
        private SimplePose? latestRawHomographicalPoseWrapper;

        const float ppiSmoothTime = 1f;
        private const float minPositivePpi = 1e-4f; // We want to avoid any pose to have 0 PPI.
        private const float minPpiRecipricol = 1f / 8; // PPI should never go above 8, even for homographical cases.

        private float smoothedPpiVelocity;
        private float smoothedPpi = -1; // This is to store a smoothed PPI.

        private float smoothedHomographicalPpiVelocity;
        private float smoothedHomographicalPpi = -1; // This is to store a smoothed PPI for homological pose.

        private struct SmoothedPoseNode
        {
            private TickDebouncedBoolean isDetected;
            private OneEuroFilterVector2 positionFilter;

            public SmoothedPoseNode(TickDebouncedBoolean.DebounceConfig debounceConfig,
                OneEuroFilterConfig oneEuroFilterConfig)
            {
                isDetected = new TickDebouncedBoolean(debounceConfig);
                positionFilter = new OneEuroFilterVector2(oneEuroFilterConfig);
            }

#if UNITY_EDITOR
            internal void SetOneEuroFilterConfig(OneEuroFilterConfig newConfig)
            {
                positionFilter.Config = newConfig;
            }
#endif

            public void Reset(PoseNode node)
            {
                isDetected.Reset();
                isDetected.Update(node.isDetected);
                positionFilter.Set(node.isDetected ? node.ToVector2() : null);
            }

            public void Update(PoseNode source, float ppi, ref PoseNode dest)
            {
                isDetected.Update(source.isDetected);
                positionFilter.Update(source.isDetected ? source.ToVector2() : null, Time.unscaledDeltaTime, ppi);
                var pos = positionFilter.FilteredValue;
                if (pos == null)
                {
                    dest.x = dest.y = 0;
                    dest.isDetected = false;
                }
                else
                {
                    dest.x = pos.Value.x;
                    dest.y = pos.Value.y;
                    dest.isDetected = isDetected.Value;
                }
            }
        }

        private readonly SmoothedPoseNode[] smoothedPoseNodes = new SmoothedPoseNode[BodyPose.nodeNumber];

        public SimplePose? GetRawPose() => latestRawPoseWrapper;
        public SimplePose? GetSmoothedPose() => !isDetected.Value ? null : latestSmoothedPoseWrapper;

        public SimplePose? GetSmoothedHomographicalPose() =>
            !isDetected.Value ? null : latestSmoothedHomographicalPoseWrapper;

        public SimplePose? GetRawHomographicalPose() => !isDetected.Value ? null : latestRawHomographicalPoseWrapper;

        public PoseSmoothEngine(TickDebouncedBoolean.DebounceConfig poseDebounceConfig,
            TickDebouncedBoolean.DebounceConfig poseNodeDebounceConfig, OneEuroFilterConfig oneEuroFilterConfig)
        {
            isDetected = new TickDebouncedBoolean(poseDebounceConfig);
            for (var i = 0; i < BodyPose.nodeNumber; ++i)
            {
                smoothedPoseNodes[i] = new SmoothedPoseNode(poseNodeDebounceConfig, oneEuroFilterConfig);
            }
        }

#if UNITY_EDITOR
        public void SetOneEuroFilterConfig(OneEuroFilterConfig newConfig)
        {
            for (var i = 0; i < BodyPose.nodeNumber; ++i)
            {
                smoothedPoseNodes[i].SetOneEuroFilterConfig(newConfig);
            }
        }
#endif

        public void UpdateRawPose(BodyPose? inputRaw)
        {
            BodyPose? raw = null;
            if (inputRaw is { pixelsPerInch: > 0 })
            {
                raw = inputRaw;
            }

            latestRawPoseWrapper = raw != null ? SimplePose.CreateWithBodyPose(raw) : null;
            if (raw != null) lastDetectedRawPose = raw;
            if (!isDetected.Update(raw != null) && raw == null)
            {
                // We should reset pose history.
                lastDetectedRawPose = smoothedBodyPose = null;
            }

            if (raw == null || smoothedBodyPose != null) return;
            // smoothed == null means that it was reset.
            // Let's prepare the pose nodes.
            smoothedBodyPose = (BodyPose)raw.Clone();
            for (var i = 0; i < BodyPose.nodeNumber; ++i)
            {
                smoothedPoseNodes[i].Reset(raw.nodes[i]);
            }
        }

        // This is called every frame, by BodyPoseController.
        public void Update()
        {
            if (smoothedBodyPose == null || lastDetectedRawPose == null)
            {
                smoothedPpiVelocity = 0;
                return;
            }

            var ppi = Mathf.Max(minPositivePpi, smoothedBodyPose.pixelsPerInch);
            for (var i = 0; i < BodyPose.nodeNumber; ++i)
            {
                smoothedPoseNodes[i].Update(lastDetectedRawPose.nodes[i], ppi, ref smoothedBodyPose.nodes[i]);
            }

            smoothedBodyPose.InvalidatePpi();
            ppi = Mathf.Max(minPositivePpi, smoothedBodyPose.pixelsPerInch);
            if (smoothedPpi <= 0)
            {
                smoothedPpi = ppi;
            }
            else
            {
                smoothedPpi = 1 / Mathf.Max(minPpiRecipricol,
                    Mathf.SmoothDamp(1 / smoothedPpi, 1 / ppi, ref smoothedPpiVelocity, ppiSmoothTime, Mathf.Infinity,
                        Time.unscaledDeltaTime));
            }

            latestSmoothedPoseWrapper = SimplePose.CreateWithBodyPose(smoothedBodyPose, smoothedPpi);
        }

        private void UpdateHomography(HomographicalTransform transform, bool smoothPpi, BodyPose? sourceBodyPose,
            ref BodyPose? bodyPoseClone, out SimplePose? poseWrapper)
        {
            if (sourceBodyPose == null)
            {
                poseWrapper = null;
                if (smoothPpi)
                {
                    smoothedHomographicalPpiVelocity = 0;
                }

                return;
            }

            bodyPoseClone = (BodyPose)sourceBodyPose.Clone();

            for (var i = 0; i < BodyPose.nodeNumber; ++i)
            {
                transform.Transform(ref bodyPoseClone.nodes[i]);
            }

            bodyPoseClone.InvalidatePpi();

            var ppi = Mathf.Max(minPositivePpi, bodyPoseClone.pixelsPerInch);
            if (smoothPpi)
            {
                if (smoothedHomographicalPpi < 0)
                {
                    smoothedHomographicalPpi = ppi;
                }
                else
                {
                    ppi = smoothedHomographicalPpi = 1 / Mathf.Max(minPpiRecipricol,
                        Mathf.SmoothDamp(1 / smoothedHomographicalPpi, 1 / ppi, ref smoothedHomographicalPpiVelocity,
                            ppiSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime));
                }
            }

            poseWrapper = SimplePose.CreateWithBodyPose(bodyPoseClone, ppi);
        }

        public void UpdateHomography(HomographicalTransform transform)
        {
            UpdateHomography(transform, false, lastDetectedRawPose, ref rawHomographicalBodyPose,
                out latestRawHomographicalPoseWrapper);
            UpdateHomography(transform, true, smoothedBodyPose, ref smoothedHomographicalBodyPose,
                out latestSmoothedHomographicalPoseWrapper);
        }
    }
}
