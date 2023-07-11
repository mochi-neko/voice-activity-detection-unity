#nullable enable
using System;
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
        /// Notice that this array is shared and reused.
        /// </summary>
        public readonly float[] buffer;

        /// <summary>
        /// Effective length of voice segment data in buffer.
        /// </summary>
        public readonly int length;

        /// <summary>
        /// Creates a new instance of <see cref="VoiceSegment"/>.
        /// </summary>
        /// <param name="buffer">Buffer array of voice segment.</param>
        /// <param name="length">Effective length of voice segment data in buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException">length must be short than buffer size.</exception>
        public VoiceSegment(float[] buffer, int length)
        {
            if (length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            
            this.buffer = buffer;
            this.length = length;
        }

        /// <summary>
        /// Calculates the volume (root mean square) of this voice segment.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Copies deeply this voice segment data.
        /// </summary>
        /// <returns></returns>
        public VoiceSegment Copy()
        {
            var copy = new float[length];
            Array.Copy(buffer, copy, length);
            return new VoiceSegment(copy, length);
        }
    }
}