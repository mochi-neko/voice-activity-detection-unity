#nullable enable
using System;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    /// <summary>
    /// A sample of voice activity detection by CumulativeVoiceActivityDetector.
    /// </summary>
    internal sealed class VADCamulationSample : MonoBehaviour
    {
        [SerializeField]
        private AudioSource? audioSource = null;

        private UnityMicrophoneProxy? proxy;
        private IVoiceActivityDetector? vad;

        private void Start()
        {
            if (audioSource == null)
            {
                throw new NullReferenceException(nameof(audioSource));
            }

            Application.targetFrameRate = 60;

            proxy = new UnityMicrophoneProxy();

            IVoiceSource source = new UnityMicrophoneSource(proxy);
            var buffer = new AudioClipBuffer(
                maxSampleLength: (int)(15f * source.SamplingRate),
                frequency: source.SamplingRate);

            vad = new CumulativeVoiceActivityDetector(
                source,
                buffer,
                activeVolumeThreshold: 0.007f,
                activeChargeTimeRate: 2f,
                maxChargeTimeSeconds: 2f,
                minCumulatedTimeSeconds: 0.3f,
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
