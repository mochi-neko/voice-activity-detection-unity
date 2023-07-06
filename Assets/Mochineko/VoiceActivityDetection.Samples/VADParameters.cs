#nullable enable
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    /// <summary>
    /// A sample implementation of voice activity detection parameters as <see cref="ScriptableObject"/>.
    /// Please refer to <see cref="QueueingVoiceActivityDetector"/> constructor.
    /// </summary>
    [CreateAssetMenu(menuName = "Mochineko/Voice Activity Detection - Sample/Create VADParameters", fileName = "VADParameters")]
    internal sealed class VADParameters : ScriptableObject
    {
        [SerializeField]
        private float activeVolumeThreshold = 0.01f;
        public float ActiveVolumeThreshold => activeVolumeThreshold;

        [SerializeField]
        private float maxQueueingTimeSeconds = 1f;
        public float MaxQueueingTimeSeconds => maxQueueingTimeSeconds;

        [SerializeField]
        private float minQueueingTimeSeconds = 0.5f;
        public float MinQueueingTimeSeconds => minQueueingTimeSeconds;

        [SerializeField]
        private float activationRateThreshold = 0.6f;
        public float ActivationRateThreshold => activationRateThreshold;

        [SerializeField]
        private float inactivationRateThreshold = 0.4f;
        public float InactivationRateThreshold => inactivationRateThreshold;

        [SerializeField]
        private float activationIntervalSeconds = 0.5f;
        public float ActivationIntervalSeconds => activationIntervalSeconds;

        [SerializeField]
        private float inactivationIntervalSeconds = 0.5f;
        public float InactivationIntervalSeconds => inactivationIntervalSeconds;

        [SerializeField]
        private float maxActiveDurationSeconds = 10f;
        public float MaxActiveDurationSeconds => maxActiveDurationSeconds;
    }
}