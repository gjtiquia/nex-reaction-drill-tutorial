#nullable enable

using UnityEngine;

namespace Nex.Essentials.Examples.ReactionDrill
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ReactionDrillHandRenderer : MonoBehaviour
    {
        [SerializeField] private HandType handType;
        [SerializeField] private ReactionDrillGameConfigs configs = null!;

        private SpriteRenderer spriteRenderer = null!;

        protected void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = configs.GetColor(handType);
        }
    }
}
