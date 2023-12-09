#nullable enable
using System;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Components
{
    /// <summary>
    /// Parameter set for cumulative logic.
    /// </summary>
    [Serializable]
    public sealed class CumulativeLogicParameters
    {
        [SerializeField, Tooltip("Threshold of active volume (root mean square) of voice data.")]
        private float activeVolumeThreshold = 0.007f;

        /// <summary>
        /// Threshold of active volume (root mean square) of voice data.
        /// </summary>
        public float ActiveVolumeThreshold => activeVolumeThreshold;

        [SerializeField, Tooltip("Rate to charge time for active voice.")]
        private float activeChargeTimeRate = 2f;

        /// <summary>
        /// Rate to charge time for active voice.
        /// </summary>
        public float ActiveChargeTimeRate => activeChargeTimeRate;

        [SerializeField, Tooltip("Maximum and initial charge time in seconds.")]
        private float maxChargeTimeSeconds = 2f;

        /// <summary>
        /// Maximum and initial charge time in seconds.
        /// </summary>
        public float MaxChargeTimeSeconds => maxChargeTimeSeconds;

        [SerializeField, Tooltip("Minimum of cumulated time in seconds to buffer.")]
        private float minCumulatedTimeSeconds = 0.3f;

        /// <summary>
        /// Minimum of cumulated time in seconds to buffer.
        /// </summary>
        public float MinCumulatedTimeSeconds => minCumulatedTimeSeconds;

        [SerializeField, Tooltip("Maximum of cumulated time in seconds to buffer.")]
        private float maxCumulatedTimeSeconds = 10f;

        /// <summary>
        /// Maximum of cumulated time in seconds to buffer.
        /// </summary>
        public float MaxCumulatedTimeSeconds => maxCumulatedTimeSeconds;
    }
}
