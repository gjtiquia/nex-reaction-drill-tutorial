#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nex.Essentials.MotionNode;
using UnityEngine;

namespace Nex.Essentials
{
    public class MotionNodeController : MonoBehaviour
    {
        public enum SourceType
        {
            BodyPoseController = 0,
            MotionNodeController = 1,
        }

        [Header("Source")]
        [SerializeField] private SourceType source = SourceType.BodyPoseController;

        [Header("Source Body Pose Controller")]
        [SerializeField] private BodyPoseController bodyPoseController = null!;

        [SerializeField] private int poseIndex;
        [SerializeField] private BodyPoseController.PoseFlavor poseFlavor = BodyPoseController.PoseFlavor.Raw;

        [Header("Input Node")]
        [SerializeField] private SimplePose.NodeIndex nodeIndex;

        public enum OriginType
        {
            Frame,
            Node,
        }

        [Header("Origin")]
        [SerializeField] private OriginType origin = OriginType.Frame;

        [SerializeField] private SimplePose.NodeIndex originNodeIndex = SimplePose.NodeIndex.Chest;

        public enum FrameOriginType
        {
            BottomLeft,
            BottomRight,
            TopLeft,
            TopRight,
            Center,
        }

        [SerializeField] private FrameOriginType frameOrigin = FrameOriginType.BottomLeft;

        public enum UnitType
        {
            AspectNormalized,
            Inches,
            Meters,
        }

        [Header("Unit")]
        [Tooltip("Convert the coordinate unit before applying value transformations")] [SerializeField]
        private UnitType unit = UnitType.AspectNormalized;

        [SerializeField] private bool useSmoothedPpi = true;
        [SerializeField] private OneEuroFilterConfig ppiSmoothingConfig = new(1, 0.001f, 1);
        private OneEuroFilterFloat ppiSmoothingFilter = null!;
        private const float InchesToMeters = 0.0254f;

        [Header("Motion Node")]
        [SerializeField] private MotionNodeController sourceMotionNodeController = null!;

        [Space(10)] [SerializeReference, Tooltip("List of transformations to apply, from top to bottom")]
        private List<ValueTransformer> valueTransformations = new() { new OneEuroFilter() };

        [SerializeReference, Tooltip("List of output destinations")]
        private List<ValueDestination> valueDestinations = new();

        private Vector2? lastInputNodeValue;
        private Vector2? lastOriginNodeValue;
        private float lastPpiValue;
        private bool isDetected;

        private readonly AsyncReactiveProperty<Vector2> output = new(Vector2.zero);

        public Vector2 Raw { get; private set; }

        public Vector2 Value => output.Value;
        public IUniTaskAsyncEnumerable<Vector2> ValueStream => output;

        [Obsolete] public Vector2 NodePosition => output.Value;
        [Obsolete] public IUniTaskAsyncEnumerable<Vector2> NodePositionStream => output;

        // Public setters for properties.
        public SourceType Source
        {
            get => source;
            set => source = value;
        }

        public BodyPoseController BodyPoseController
        {
            get => bodyPoseController;
            set => bodyPoseController = value;
        }

        public int PoseIndex
        {
            get => poseIndex;
            set => poseIndex = value;
        }

        public BodyPoseController.PoseFlavor PoseFlavor
        {
            get => poseFlavor;
            set => poseFlavor = value;
        }

        public SimplePose.NodeIndex NodeIndex
        {
            get => nodeIndex;
            set => nodeIndex = value;
        }

        public OriginType Origin
        {
            get => origin;
            set => origin = value;
        }

        public SimplePose.NodeIndex OriginNodeIndex
        {
            get => originNodeIndex;
            set => originNodeIndex = value;
        }

        public FrameOriginType FrameOrigin
        {
            get => frameOrigin;
            set => frameOrigin = value;
        }

        public UnitType Unit
        {
            get => unit;
            set => unit = value;
        }

        public bool UseSmoothedPpi
        {
            get => useSmoothedPpi;
            set => useSmoothedPpi = value;
        }

        public OneEuroFilterConfig PpiSmoothingConfig
        {
            get => ppiSmoothingConfig;
            set => ppiSmoothingConfig = value;
        }

        public MotionNodeController SourceMotionNodeController
        {
            get => sourceMotionNodeController;
            set => sourceMotionNodeController = value;
        }

        public List<ValueTransformer> ValueTransformations
        {
            get => valueTransformations;
            set => valueTransformations = value;
        }

        public List<ValueDestination> ValueDestinations
        {
            get => valueDestinations;
            set => valueDestinations = value;
        }

        private void Start()
        {
            valueTransformations.ForEach(transformer => transformer.Start());

            // Initialize smooth filters
            ppiSmoothingFilter = new OneEuroFilterFloat(ppiSmoothingConfig);
        }

        private Vector2 GetFrameOrigin()
        {
            const float frameWidth = 16f / 9;
            const float frameHeight = 1f;
            return frameOrigin switch
            {
                FrameOriginType.BottomLeft => new Vector2(0, 0),
                FrameOriginType.BottomRight => new Vector2(frameWidth, 0),
                FrameOriginType.TopLeft => new Vector2(0, frameHeight),
                FrameOriginType.TopRight => new Vector2(frameWidth, frameHeight),
                FrameOriginType.Center => new Vector2(frameWidth * 0.5f, frameHeight * 0.5f),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private Vector2? ExtractBodyPoseRawValue()
        {
            if (bodyPoseController.TryGetBodyPose(poseIndex, poseFlavor, out var simplePose))
            {
                lastInputNodeValue = simplePose[nodeIndex];
                lastPpiValue = simplePose.pixelsPerInch;

                if (origin == OriginType.Node)
                {
                    lastOriginNodeValue = simplePose[originNodeIndex];
                }
            }

            var ppi = useSmoothedPpi ? ppiSmoothingFilter.Update(lastPpiValue, Time.unscaledDeltaTime) : lastPpiValue;
            var inputPos = origin switch
            {
                OriginType.Frame => lastInputNodeValue - GetFrameOrigin(),
                OriginType.Node => lastInputNodeValue - lastOriginNodeValue,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (inputPos == null) return null;
            return TranslateInputToUnit(inputPos.Value, ppi);
        }

        private Vector2? ExtractMotionNodeRawValue()
        {
            return sourceMotionNodeController.Compute();
        }

        private int frameCount = -1;

        private Vector2 Compute()
        {
            if (Time.frameCount == frameCount)
            {
                return output.Value;
            }

            frameCount = Time.frameCount;

            valueTransformations.ForEach(transformer => transformer.Update());

            // Get the source now.
            var sourceValue = source switch
            {
                SourceType.BodyPoseController => ExtractBodyPoseRawValue(),
                SourceType.MotionNodeController => ExtractMotionNodeRawValue(),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (sourceValue == null)
            {
                // There is really nothing to update.
                // Just use the last value.
                return output.Value;
            }

            Raw = sourceValue.Value;
            // Apply the list of value transformations
            var acc = Raw;
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var transformer in valueTransformations)
            {
                if (!transformer.enabled) continue;
                acc = transformer.Process(acc);
            }

            output.Value = acc;
            valueDestinations.ForEach(destination =>
            {
                if (destination.enabled) destination.Apply(acc);
            });

            return acc;
        }

        private void Update()
        {
            Compute(); // This will dedup-per frame.
        }

        private Vector2 TranslateInputToUnit(Vector2 inputPos, float ppi)
        {
            return inputPos * unit switch
            {
                UnitType.AspectNormalized => 1,
                UnitType.Inches => 1 / ppi,
                UnitType.Meters => 1 / ppi * InchesToMeters,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
            };
        }

        public T? GetValueTransformer<T>(string? description = null) where T : ValueTransformer
        {
            foreach (var transformer in valueTransformations)
            {
                if (transformer is not T matchedTransformer) continue;
                if (description != null && matchedTransformer.description != description) continue;
                return matchedTransformer;
            }

            return null;
        }

        public T? GetValueDestination<T>(string? description = null) where T : ValueDestination
        {
            foreach (var destination in valueDestinations)
            {
                if (destination is not T matchedDestination) continue;
                if (description != null && matchedDestination.description != description) continue;
                return matchedDestination;
            }

            return null;
        }

        public T? GetValueTransformer<T>(int? index = null) where T : ValueTransformer
        {
            if (index.HasValue)
            {
                if (index.Value >= valueTransformations.Count)
                {
                    return null;
                }

                // Check if the transformer is of type T and matches the label
                if (valueTransformations[index.Value] is T matchedTransformer)
                {
                    return matchedTransformer;
                }
            }
            else
            {
                foreach (var v in valueTransformations)
                {
                    if (v is T matchedTransformer)
                    {
                        return matchedTransformer;
                    }
                }
            }

            return null;
        }

        public T? GetValueDestinations<T>(int? index = null) where T : ValueDestination
        {
            if (index.HasValue)
            {
                if (index.Value >= valueDestinations.Count)
                {
                    return null;
                }

                // Check if the destination is of type T and matches the label
                if (valueDestinations[index.Value] is T matchedDestination)
                {
                    return matchedDestination;
                }
            }
            else
            {
                foreach (var d in valueDestinations)
                {
                    if (d is T matchedDestination)
                    {
                        return matchedDestination;
                    }
                }
            }

            return null;
        }
    }
}
