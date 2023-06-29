#nullable enable
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    /// <summary>
    /// A sample of voice activity detection as a component.
    /// </summary>
    internal sealed class VoiceActivityDetectionSample : MonoBehaviour
    {
        [SerializeField]
        private float volumeThreshold = 0.01f;
        [SerializeField]
        private float intervalThresholdSeconds = 1f;

        private IVoiceActivityDetector? vad;

        private void Start()
        {
            vad = new SimpleVoiceActivityDetector(
                source: new UnityMicrophoneSource(),
                buffer: new NullVoiceBuffer(),
                volumeThreshold,
                intervalThresholdSeconds);

            vad
                .IsActive
                .Subscribe(isActive => Log.Debug("[VAD.Sample] IsActive: {0}", isActive))
                .AddTo(this);
        }

        private void OnDestroy()
        {
            vad?.Dispose();
        }

        private void Update()
        {
            vad?.Update();
        }
    }
}