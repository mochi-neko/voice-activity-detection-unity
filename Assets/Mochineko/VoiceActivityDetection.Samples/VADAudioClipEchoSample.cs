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
        private float activeVolumeThreshold = 0.01f;
        
        [SerializeField]
        private float maxQueueingTimeSeconds = 1f;

        [SerializeField]
        private float minQueueingTimeSeconds = 0.5f;
        
        [SerializeField]
        private float activationRateThreshold = 0.6f;
        
        [SerializeField]
        private float inactivationRateThreshold = 0.4f;
        
        [SerializeField]
        private float activationIntervalSeconds = 0.5f;
        
        [SerializeField]
        private float inactivationIntervalSeconds = 0.5f;
        
        [SerializeField]
        private float maxActiveDurationSeconds = 10f;

        [SerializeField]
        private AudioSource? audioSource = null;

        private IVoiceActivityDetector? vad;

        private void Start()
        {
            if (audioSource == null)
            {
                throw new NullReferenceException(nameof(audioSource));
            }
            
            IVoiceSource source = new UnityMicrophoneSource();
            var buffer = new AudioClipBuffer(
                maxSampleLength: (int)(maxActiveDurationSeconds * source.SamplingRate),
                frequency: source.SamplingRate);
            
            vad = new QueueingVoiceActivityDetector(
                source,
                buffer,
                maxQueueingTimeSeconds,
                minQueueingTimeSeconds,
                activeVolumeThreshold,
                activationRateThreshold,
                inactivationRateThreshold,
                activationIntervalSeconds,
                inactivationIntervalSeconds,
                maxActiveDurationSeconds);

            vad
                .IsActive
                .Subscribe(isActive => Log.Debug("[VAD.Sample] IsActive: {0}", isActive))
                .AddTo(this);

            buffer
                .OnInactive
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
        }

        private void Update()
        {
            vad?.Update();
        }
    }
}