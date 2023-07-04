#nullable enable
using System;

namespace Mochineko.VoiceActivityDetection
{
    internal readonly struct VoiceSegmentActivity
    {
        public readonly bool isActive;
        public readonly float timeSeconds;

        public VoiceSegmentActivity(
            bool isActive,
            int length,
            int samplingRate,
            int channels)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            
            if (samplingRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(samplingRate));
            }

            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels));
            }
            
            this.isActive = isActive;
            this.timeSeconds = (float)length / samplingRate / channels;
        }
    }
}