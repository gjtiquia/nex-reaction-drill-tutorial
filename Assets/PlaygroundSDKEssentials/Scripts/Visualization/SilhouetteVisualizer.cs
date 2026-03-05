#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Mesh = UnityEngine.Mesh;

namespace Nex.Essentials
{
    public class SilhouetteVisualizer : MonoBehaviour
    {
        [SerializeField] private Camera silhouetteCamera = null!;
        [SerializeField] private Vector2Int textureSize = new(1920, 1080);

        [SerializeField] private MdkController mdkController = null!;
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private BodyPoseController.PoseFlavor flavor = BodyPoseController.PoseFlavor.Smoothed;

        [SerializeField] private SilhouetteRenderer silhouetteRenderer = null!;

        private RenderTexture activeTexture = null!;
        private VertexHelper vertexHelper = null!;
        private Mesh activeMesh = null!;
        private readonly List<IDisposable> activeSubscriptions = new();

        public Texture ActiveTexture => activeTexture;

        private void Awake()
        {
            activeMesh = new Mesh();
        }

        private void OnDestroy()
        {
            Destroy(activeMesh);
        }

        private void OnEnable()
        {
            activeTexture = new RenderTexture(textureSize.x, textureSize.y, 0, GraphicsFormat.R8G8B8A8_UNorm);
            activeTexture.Create();
            silhouetteCamera.targetTexture = activeTexture;
            vertexHelper = new VertexHelper();
        }

        private void OnDisable()
        {
            Destroy(activeTexture);
            if (silhouetteCamera != null) silhouetteCamera.targetTexture = null;
            activeTexture = null!;
            vertexHelper.Dispose();
            vertexHelper = null!;
            foreach (var subscription in activeSubscriptions)
            {
                subscription.Dispose();
            }

            activeSubscriptions.Clear();
        }

        private void Update()
        {
            while (activeSubscriptions.Count < mdkController.PlayerCount)
            {
                activeSubscriptions.Add(SubscribeBodyPoseStream(activeSubscriptions.Count));
            }
            while (activeSubscriptions.Count > mdkController.PlayerCount)
            {
                var lastIndex = activeSubscriptions.Count - 1;
                activeSubscriptions[lastIndex].Dispose();
                activeSubscriptions.RemoveAt(lastIndex);
            }
        }

        private IDisposable SubscribeBodyPoseStream(int index)
        {
            return bodyPoseController.GetBodyPoseStream(index, flavor).Subscribe(
                pose =>
                {
                    // Update the silhouette renderer with the new pose.
                    silhouetteRenderer.SetPose(index, pose);
                });
        }
    }
}
