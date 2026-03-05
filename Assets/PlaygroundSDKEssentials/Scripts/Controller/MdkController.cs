#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Jazz;
using UnityEngine;
using UnityEngine.Events;

namespace Nex.Essentials
{
    public class MdkController : MonoBehaviour
    {
        [SerializeField] private CvDetectionManager cvDetectionManager = null!;

        public CvDetectionManager CvDetectionManager
        {
            get => cvDetectionManager;
            set => cvDetectionManager = value;
        }

        [SerializeField] private BodyPoseDetectionManager bodyPoseDetectionManager = null!;

        public BodyPoseDetectionManager BodyPoseDetectionManager
        {
            get => bodyPoseDetectionManager;
            set => bodyPoseDetectionManager = value;
        }

        private readonly UnityEvent<FrameInformation> frameInformationPublisher = new();

        public IUniTaskAsyncEnumerable<FrameInformation> GetFrameInformationStream() =>
            frameInformationPublisher.OnInvokeAsAsyncEnumerable(this.GetCancellationTokenOnDestroy());

        private readonly UnityEvent<BodyPoseDetection> bodyPoseDetectionPublisher = new();

        public IUniTaskAsyncEnumerable<BodyPoseDetection> GetBodyPoseDetectionStream() =>
            bodyPoseDetectionPublisher.OnInvokeAsAsyncEnumerable(this.GetCancellationTokenOnDestroy());

        public Vector2[] positions { get; private set; } = { };

        public int PlayerCount => positions.Length;

        private enum State
        {
            Stopped,
            Starting,
            Running,
        }

        private State state = State.Stopped;

        public async UniTask StartRunning()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            switch (state)
            {
                case State.Stopped:
                    // Do the normal routine.
                    break;
                case State.Starting:
                    // This is pretty bad...
                    Debug.LogWarning("MDKExt: Reentrant call to StartRunning.");
                    await UniTask.WaitUntil(() => state == State.Running, cancellationToken: cancellationToken);
                    return;
                case State.Running:
                    return; // Nothing to do.
                default:
                    throw new ArgumentOutOfRangeException();
            }

            state = State.Starting;
            bodyPoseDetectionManager.shouldDetect = true;
            cvDetectionManager.StartRunning();

            firstCameraFrameReadySource = new UniTaskCompletionSource();
            firstBodyPoseReadySource = new UniTaskCompletionSource();
            var firstCameraFrameReadyTask = firstCameraFrameReadySource.Task;
            var firstBodyPoseReadyTask = firstBodyPoseReadySource.Task;
            cvDetectionManager.captureCameraFrame += HandleCameraFrame;
            bodyPoseDetectionManager.captureAspectNormalizedDetection += HandleBodyPoseDetection;
            await firstCameraFrameReadyTask.AttachExternalCancellation(cancellationToken);
            await firstBodyPoseReadyTask.AttachExternalCancellation(cancellationToken);

            state = State.Running;
        }

        public void StopRunning()
        {
            if (state == State.Stopped) return;
            bodyPoseDetectionManager.shouldDetect = false;
            cvDetectionManager.StopRunning();

            cvDetectionManager.captureCameraFrame -= HandleCameraFrame;
            bodyPoseDetectionManager.captureAspectNormalizedDetection -= HandleBodyPoseDetection;

            state = State.Stopped;
        }

        public void SetPlayerPositions(Vector2[] newPositions)
        {
            positions = newPositions;
            // Update player positions.
            var n = positions.Length;
            var target = cvDetectionManager.playerPositions;
            var common = n < target.Count ? n : target.Count;
            for (var i = 0; i < common; ++i)
            {
                target[i] = new Vector2(positions[i].x, 1 - positions[i].y);
            }

            for (var i = common; i < n; ++i)
            {
                target.Add(new Vector2(positions[i].x, 1 - positions[i].y));
            }

            while (target.Count > n) target.RemoveAt(target.Count - 1);

            cvDetectionManager.numOfPlayers = n;
        }

        public bool DewarpLocked
        {
            get => DewarpWrapper.GetDewarpLocked();
            set => DewarpWrapper.SetDewarpLocked(value);
        }

        public bool EnableConsistency
        {
            get => bodyPoseDetectionManager.trackingConfig.enableConsistency;
            set
            {
                if (EnableConsistency == value) return;

                bodyPoseDetectionManager.trackingConfig.enableConsistency = value;
                Debug.Log($"Consistency updated: {value}");
            }
        }

        private UniTaskCompletionSource? firstCameraFrameReadySource;
        private UniTaskCompletionSource? firstBodyPoseReadySource;

        private void HandleCameraFrame(FrameInformation frameInfo)
        {
            if (firstCameraFrameReadySource != null)
            {
                firstCameraFrameReadySource.TrySetResult();
                firstCameraFrameReadySource = null;
            }

            frameInformationPublisher.Invoke(frameInfo);
        }

        private void HandleBodyPoseDetection(BodyPoseDetectionResult result)
        {
            if (firstBodyPoseReadySource != null)
            {
                firstBodyPoseReadySource.TrySetResult();
                firstBodyPoseReadySource = null;
            }

            bodyPoseDetectionPublisher.Invoke(result.original);
        }
    }
}
