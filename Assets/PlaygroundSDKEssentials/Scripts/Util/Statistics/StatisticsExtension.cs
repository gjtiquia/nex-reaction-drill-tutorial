#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nex.Essentials
{
    /// <summary>
    /// This class provides extension methods to deal with statistics.
    /// </summary>
    public static class StatisticsExtension
    {

        public static double Mean(this double[] values)
        {
            var n = values.Length;
            double sum = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sum += value;
            }
            return sum / n;
        }

        public static double StandardDeviation(this double[] values)
        {
            var n = values.Length;
            double sumValue = 0;
            double sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
            }
            var mean = sumValue / n;
            return Math.Sqrt(sumSquareValue / n - mean * mean);
        }

        public static (double mean, double deviation) MeanAndDeviation(this double[] values)
        {
            var n = values.Length;
            double sumValue = 0;
            double sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
            }
            var mean = sumValue / n;
            return (mean, Math.Sqrt(sumSquareValue / n - mean * mean));
        }

        public static double Mean(this IEnumerable<double> values)
        {
            var n = 0;
            double sum = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sum += value;
                ++n;
            }
            return sum / n;
        }

        public static double StandardDeviation(this IEnumerable<double> values)
        {
            var n = 0;
            double sumValue = 0;
            double sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
                ++n;
            }
            var mean = sumValue / n;
            return Math.Sqrt(sumSquareValue / n - mean * mean);
        }

        public static (double mean, double deviation) MeanAndDeviation(this IEnumerable<double> values)
        {
            var n = 0;
            double sumValue = 0;
            double sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
                ++n;
            }
            var mean = sumValue / n;
            return (mean, Math.Sqrt(sumSquareValue / n - mean * mean));
        }

        public static float Mean(this float[] values)
        {
            var n = values.Length;
            float sum = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sum += value;
            }
            return sum / n;
        }

        public static float StandardDeviation(this float[] values)
        {
            var n = values.Length;
            float sumValue = 0;
            float sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
            }
            var mean = sumValue / n;
            return Mathf.Sqrt(sumSquareValue / n - mean * mean);
        }

        public static (float mean, float deviation) MeanAndDeviation(this float[] values)
        {
            var n = values.Length;
            float sumValue = 0;
            float sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
            }
            var mean = sumValue / n;
            return (mean, Mathf.Sqrt(sumSquareValue / n - mean * mean));
        }

        public static float Mean(this IEnumerable<float> values)
        {
            var n = 0;
            float sum = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sum += value;
                ++n;
            }
            return sum / n;
        }

        public static float StandardDeviation(this IEnumerable<float> values)
        {
            var n = 0;
            float sumValue = 0;
            float sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
                ++n;
            }
            var mean = sumValue / n;
            return Mathf.Sqrt(sumSquareValue / n - mean * mean);
        }

        public static (float mean, float deviation) MeanAndDeviation(this IEnumerable<float> values)
        {
            var n = 0;
            float sumValue = 0;
            float sumSquareValue = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                sumValue += value;
                sumSquareValue += value * value;
                ++n;
            }
            var mean = sumValue / n;
            return (mean, Mathf.Sqrt(sumSquareValue / n - mean * mean));
        }
    }
}
