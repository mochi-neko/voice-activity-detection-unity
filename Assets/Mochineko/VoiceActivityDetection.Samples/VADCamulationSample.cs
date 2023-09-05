#nullable enable
using System;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    internal sealed class VADCamulationSample : MonoBehaviour
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

            vad = new CumulativeVoiceActivityDetector(
                source,
                buffer,
                activeVolumeThreshold: 0.01f,
                activeChargeTimeRate: 2f,
                maxChargeTimeSeconds: 2f,
                effectiveCumulatedTimeThresholdSeconds: 0.5f,
                maxCumulatedTimeSeconds: 10f
            );

            vad
                .VoiceIsActive
                .Subscribe(isActive => Log.Debug("[VAD.Sample] IsActive: {0}", isActive))
                .AddTo(this);

            buffer
                .OnVoiceInactive
                .Subscribe(clip =>
                {
                    Log.Info("[VAD.Sample] OnInactive and receive AudioClip and play.");
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
