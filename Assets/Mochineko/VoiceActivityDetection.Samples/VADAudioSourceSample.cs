#nullable enable
using System;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    /// <summary>
    /// A sample of voice activity detection as a component.
    /// Input UnityEngine.AudioSource and output only log.
    /// </summary>
    internal sealed class VADAudioSourceSample : MonoBehaviour
    {
        [SerializeField]
        private VADParameters? parameters = null;

        [SerializeField]
        private AudioSource? audioSource = null;

        private IVoiceActivityDetector? vad;
        private UnityMicrophoneProxy? proxy;
        private UnityAudioSource? source;

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

            Application.targetFrameRate = 60;

            source = new UnityAudioSource(readBufferSize: 2048, mute: false);

            vad = new QueueingVoiceActivityDetector(
                source: source,
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
                .Subscribe(isActive => Log.Info("[VAD.Sample] IsActive: {0}", isActive))
                .AddTo(this);

            // Set microphone as audio source
            proxy = new UnityMicrophoneProxy();
            var audioClip = proxy.AudioClip;
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        private void OnDestroy()
        {
            proxy?.Dispose();
            vad?.Dispose();
        }

        private void Update()
        {
            vad?.Update();
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            source?.OnAudioFilterRead(data, channels);
        }
    }
}
