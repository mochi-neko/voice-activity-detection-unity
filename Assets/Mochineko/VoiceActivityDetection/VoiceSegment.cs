#nullable enable
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Voice segment data.
    /// </summary>
    public readonly struct VoiceSegment
    {
        /// <summary>
        /// Buffer array of voice segment.
        /// </summary>
        public readonly float[] buffer;

        /// <summary>
        /// Effective length of voice segment data in buffer.
        /// </summary>
        public readonly int length;

        public VoiceSegment(float[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }

        public float Volume()
        {
            var sum = 0f;
            for (var i = 0; i < length; i++)
            {
                var sample = buffer[i];
                sum += sample * sample;
            }

            return Mathf.Sqrt(sum / length); // Root mean square
        }
    }
}