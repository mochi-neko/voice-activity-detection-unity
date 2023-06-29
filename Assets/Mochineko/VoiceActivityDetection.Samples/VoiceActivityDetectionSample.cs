#nullable enable
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    internal sealed class VoiceActivityDetectionSample : MonoBehaviour
    {
        [SerializeField]
        private float volumeThreshold = 0.01f;

        private SimpleVoiceActivityDetection? vad;

        private void Start()
        {
            vad = new SimpleVoiceActivityDetection(
                input: new UnityMicrophoneInput(),
                output: new NullVoiceOutput(),
                volumeThreshold);

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