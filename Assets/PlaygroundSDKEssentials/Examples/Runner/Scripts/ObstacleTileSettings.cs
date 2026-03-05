using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nex.Essentials.Examples.Runner
{
    [Serializable]
    public struct NextTileProbability
    {
        public ObstacleTileSettings tileSettings;
        public float probability;
    }

    [CreateAssetMenu(fileName = "ObstacleTileSettings", menuName = "MyGame/Obstacle Settings")]
    public class ObstacleTileSettings : ScriptableObject
    {
        public Vector3[] obstaclePositions;
        public Obstacle[] obstaclePrefabs;
        public Vector3[] bombsPositions;
        public Bomb[] bombsPrefabs;
        public NextTileProbability[] nextTileProbabilities;
    }
}
