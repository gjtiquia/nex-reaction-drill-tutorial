#nullable enable

using System;
using UnityEngine;

namespace Nex.Essentials.Examples.ReactionDrill
{
    [CreateAssetMenu(fileName = "ReactionDrillGameConfigs",
        menuName = "Nex/MdkExtensions/Examples/ReactionDrillGameConfigs", order = 0)]
    public class ReactionDrillGameConfigs : ScriptableObject
    {
        public Color leftHandColor = Color.red;
        public Color rightHandColor = Color.blue;

        public string leftHandTag = "LeftHand";
        public string rightHandTag = "RightHand";

        public int targetMaxScore = 5;
        public float targetLifeTime = 2;

        public int gameDuration = 60;  // In seconds.

        public RectInt targetRect = new(1, -3, 5, 6);
        public Vector2 targetCooldown = new(0.25f, 0.8f);

        public Color GetColor(HandType handType)
        {
            return handType switch
            {
                HandType.Left => leftHandColor,
                HandType.Right => rightHandColor,
                _ => throw new ArgumentOutOfRangeException(nameof(handType), handType, null)
            };
        }

        public string GetTag(HandType handType)
        {
            return handType switch
            {
                HandType.Left => leftHandTag,
                HandType.Right => rightHandTag,
                _ => throw new ArgumentOutOfRangeException(nameof(handType), handType, null)
            };
        }
    }
}
