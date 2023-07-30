#nullable enable
using System;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    /// <summary>
    /// A sample of voice activity detection as a component.
    /// Input UnityEngine.Microphone and output AudioClip, then play by AudioSource.
    /// </summary>
    internal sealed class VADAudioClipEchoSample : MonoBehaviour
    {
        [SerializeField]
        private VADParameters? parameters = null;

        [SerializeField]
        private AudioSource? audioSource = null;

        private UnityMicrophoneProxy? proxy;
        private IVoiceActivityDetector? vad;

        private void Start()
        {
            if (parameters == null)
            {
                throw new NullReferenceException(nameof(parameters));
            }
            if (audioSource == null)
            {
                throw new NullReferenceException(nameof(audioSource));
            }

            proxy = new UnityMicrophoneProxy();

            IVoiceSource source = new UnityMicrophoneSource(proxy);
            var buffer = new AudioClipBuffer(
                maxSampleLength: (int)(parameters.MaxActiveDurationSeconds * source.SamplingRate),
                frequency: source.SamplingRate);

            vad = new QueueingVoiceActivityDetector(
                source,
                buffer,
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

            buffer
                .OnVoiceInactive
                .Subscribe(clip =>
                {
                    Log.Debug("[VAD.Sample] OnInactive and receive AudioClip and play.");
                    audioSource.clip = clip;
                    audioSource.Play();
                })
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
