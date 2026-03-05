#nullable enable

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nex.Essentials.Test
{
    [RequireComponent(typeof(MdkController))]
    public class MdkControllerStarter : MonoBehaviour
    {
        [SerializeField] private Vector2[] playerPositions = { new(0.5f, 0.5f) };
        private MdkController mdkController = null!;

        private void Awake()
        {
            mdkController = GetComponent<MdkController>();
            mdkController.SetPlayerPositions(playerPositions);
        }

        private void Start()
        {
            mdkController.StartRunning().Forget();
        }
    }
}
