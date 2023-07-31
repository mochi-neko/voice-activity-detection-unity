#nullable enable
using System;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Activity of voice segment.
    /// </summary>
    internal readonly struct VoiceSegmentActivity
    {
        /// <summary>
        /// Whether the segment is active.
        /// </summary>
        public readonly bool isActive;

        /// <summary>
        /// Time of the segment in seconds.
        /// </summary>
        public readonly float timeSeconds;

        /// <summary>
        /// Creates a new instance of <see cref="VoiceSegmentActivity"/>.
        /// </summary>
        /// <param name="isActive">Whether the segment is active.</param>
        /// <param name="length">Effective lenght of samples in buffer array.</param>
        /// <param name="samplingRate">Sampling rate of voice data.</param>
        /// <param name="channels">Channels count of voice data.</param>
        /// <exception cref="ArgumentOutOfRangeException">length, samplingRate and channels must be positive value.</exception>
        public VoiceSegmentActivity(
            bool isActive,
            int length,
            int samplingRate,
            int channels)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "length must be positive value.");
            }

            if (samplingRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(samplingRate), samplingRate, "samplingRate must be positive value.");
            }

            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), channels, "channels must be positive value.");
            }

            this.isActive = isActive;
            this.timeSeconds = (float)length / samplingRate / channels;
        }
    }
}
