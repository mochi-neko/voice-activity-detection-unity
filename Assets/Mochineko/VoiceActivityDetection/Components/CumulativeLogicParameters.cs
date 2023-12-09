#nullable enable
using System;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Components
{
    [Serializable]
    public sealed class CumulativeLogicParameters
    {
        [SerializeField]
        private float activeVolumeThreshold = 0.007f;

        public float ActiveVolumeThreshold => activeVolumeThreshold;

        [SerializeField]
        private float activeChargeTimeRate = 2f;

        public float ActiveChargeTimeRate => activeChargeTimeRate;

        [SerializeField]
        private float maxChargeTimeSeconds = 2f;

        public float MaxChargeTimeSeconds => maxChargeTimeSeconds;

        [SerializeField]
        private float minCumulatedTimeSeconds = 0.3f;

        public float MinCumulatedTimeSeconds => minCumulatedTimeSeconds;

        [SerializeField]
        private float maxCumulatedTimeSeconds = 10f;

        public float MaxCumulatedTimeSeconds => maxCumulatedTimeSeconds;
    }
}
