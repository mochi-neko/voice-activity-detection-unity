#nullable enable
using System;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    /// <summary>
    /// A sample of voice activity detection as a component.
    /// Input UnityEngine.Microphone and output only log.
    /// </summary>
    internal sealed class VADSample : MonoBehaviour
    {
        [SerializeField]
        private VADParameters? parameters = null;

        private IVoiceActivityDetector? vad;
        private UnityMicrophoneProxy? proxy;

        private void Start()
        {
            if (parameters == null)
            {
                throw new NullReferenceException(nameof(parameters));
            }

            proxy = new UnityMicrophoneProxy();

            vad = new QueueingVoiceActivityDetector(
                source: new UnityMicrophoneSource(proxy),
                buffer: new NullVoiceBuffer(),
                parameters.MaxQueueingTimeSeconds,
                parameters.MinQueueingTimeSeconds,
                parameters.ActiveVolumeThreshold,
                parameters.ActivationRateThreshold,
                parameters.InactivationRateThreshold,
                parameters.ActivationIntervalSeconds,
                parameters.InactivationIntervalSeconds,
                parameters.MaxActiveDurationSeconds);

            vad
                .VoiceIsActive
                .Subscribe(isActive => Log.Debug("[VAD.Sample] IsActive: {0}", isActive))
                .AddTo(this);
        }

        private void OnDestroy()
        {
            vad?.Dispose();
            proxy?.Dispose();
        }

        private void Update()
        {
            vad?.Update();
        }
    }
}
