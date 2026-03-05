#nullable enable

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Complex = System.Numerics.Complex;

namespace Nex.Essentials
{

    public static class PoseComparisonUtils
    {

        private const double originalRotationPhase = Math.PI / 18.0;  // 10 degrees.

        public static void FixRotationAndScale(IReadOnlyList<float> weights, List<Vector2> inputCoordinates, IReadOnlyList<Vector2> targetCoordinates, double graceLevel = 1)
        {
            // The chest node is expected to be the origin (and the chest node should always align).
            // We want to rotate and scale the input coordinates so that they best align with the target coordinates.
            // Our transform involves scaling and rotating (along the origin / chest). In 2D Euclidean coordinate, this
            // rigid transform and all coordinates can be represented as complex numbers.
            // The transform t we are searching for can be represented as s e^i(theta), which scales all distances by s and
            // rotate all coordinates by theta radian.
            // More rigorously, assuming that we have two ordered coordinates src_i and tar_i, and we want to find t such that.
            // sum |t * src_i - tar_i|^2 is minimized.
            // Err(t) = sum conj(t * src_i - tar_i) * (t * src_i - tar_i)
            //        = sum { conj(t * src_i) * (t * src_i) - conj(t * src_i) * tar_i - conj(tar_i) * t * src_i + conj(tar_i) * tar_i) }
            //        = sum { s^2 * |src_i|^2 - s e^i(-theta) * conj(src_i) * tar_i - s e^i(theta) * src_i * conj(tar_i) + |tar_i|^2 }
            // Now, this entity Err(t), when minimized, should have 0 when differentiated at s and theta.
            // d Err(t)                                              _____                                 _____
            // -------- = 0 = 2s sum |src_i|^2 - e^i(-theta) * sum { src_i * tar_i } - e^i (theta) * sum { src_i * tar_i }     <----- (a)
            //    ds
            // d Err(t)                               _____                                            _____
            // -------- = 0 = i s e^i(-theta) * sum { src_i * tar_i } - i s e^i(theta) * sum { src_i * tar_i }
            //  d theta
            // Dividing i on both sides, we have
            //                                       _____                                          _____
            //            0 = s e^i (-theta) * sum { src_i * tar_i } - s e^i(theta) * sum { src_i * tar_i }                   <----- (b)
            // s * (a) - (b) ==>
            //            _____                                   _____
            // 2s^2 sum { src_i * src_i } - 2 s e^i(-theta) sum { src_i * tar_i } = 0
            // If s = 0, t = 0.
            // Suppose s != 0, we can multiply both sides with e^i(theta) / 2s, and we have
            //                    _____                   _____
            // s e^i(theta) sum { src_i * src_i } - sum { src_i * tar_i } = 0
            // Rearranging, and replacing s e^i(theta) with t, we have
            //            _____
            //      sum { src_i * tar_i }
            // t = -----------------------
            //            _____
            //      sum { src_i * src_i }
            // The weighted version of this is similar, except that the conj(src_i) is replaced by w_i * conj(src_i).
            var maxRotationPhase = originalRotationPhase;
            maxRotationPhase *= graceLevel;
            double denominator = 0;
            Complex unscaledTransform = Complex.Zero;
            for (var i = 0; i < inputCoordinates.Count; ++i)
            {
                var src = new Complex(inputCoordinates[i].x, inputCoordinates[i].y);
                var tar = new Complex(targetCoordinates[i].x, targetCoordinates[i].y);
                denominator += weights[i] * (src.Real * src.Real + src.Imaginary * src.Imaginary);
                unscaledTransform += Complex.Conjugate(src) * tar * weights[i];
            }

            var originalPhase = unscaledTransform.Phase;
            var clampedPhase = Math.Min(Math.Max(originalPhase, -maxRotationPhase), maxRotationPhase);
            var transform = Complex.FromPolarCoordinates(unscaledTransform.Magnitude / denominator, clampedPhase);
            // var transform = unscaledTransform / denominator;

            for (var i = 0; i < inputCoordinates.Count; ++i)
            {
                var src = new Complex(inputCoordinates[i].x, inputCoordinates[i].y);
                var transformed = transform * src;
                inputCoordinates[i] = new Vector2((float)transformed.Real, (float)transformed.Imaginary);
            }
        }

        private static double WeightedConfidenceMatching(IReadOnlyList<float> weightedConfidences, IReadOnlyList<Vector2> inputCoordinates, IReadOnlyList<Vector2> targetCoordinates)
        {
            double confidenceSum = 0;
            double runningSum = 0;

            for (int i = 0; i < inputCoordinates.Count; i++)
            {
                var weightedConfidence = weightedConfidences[i];
                confidenceSum += weightedConfidence;
                runningSum += weightedConfidence * (inputCoordinates[i].normalized - targetCoordinates[i].normalized).magnitude;
                // runningSum += weightedConfidence * Vector2.Dot(inputCoordinates[i].normalized, targetCoordinates[i].normalized);
                // runningSum += weightedConfidence * (inputCoordinates[i] - targetCoordinates[i]).magnitude;
            }

            return 1 / confidenceSum * runningSum;
        }

        public static double PoseDistanceToPercentage(IReadOnlyList<float> weightedConfidence, IReadOnlyList<Vector2> inputCoordinates, IReadOnlyList<Vector2> targetCoordinates)
        {
            return math.max(0, (-WeightedConfidenceMatching(weightedConfidence, inputCoordinates, targetCoordinates) + 1) * 100);
        }

    }
}
