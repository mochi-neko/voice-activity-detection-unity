#nullable enable
using System;
using System.Buffers;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Voice segment data.
    /// </summary>
    public readonly struct VoiceSegment : IDisposable
    {
        /// <summary>
        /// Pooled buffer array of voice segment.
        /// </summary>
        public readonly float[] buffer;

        /// <summary>
        /// Effective length of buffer array.
        /// </summary>
        public readonly int length;

        /// <summary>
        /// Volume (root mean square) of this voice segment.
        /// </summary>
        public readonly float volume;

        /// <summary>
        /// Creates a new instance of <see cref="VoiceSegment"/>.
        /// </summary>
        /// <param name="span">Span of voice segment data.</param>
        public VoiceSegment(ReadOnlySpan<float> span)
        {
            this.length = span.Length;

            this.buffer = ArrayPool<float>.Shared.Rent(this.length);
            span.CopyTo(this.buffer);

            this.volume = CalculateVolume(this.buffer, this.length);
        }

        /// <summary>
        /// Creates a new instance of <see cref="VoiceSegment"/>.
        /// </summary>
        /// <param name="firstSpan">First span of voice segment data.</param>
        /// <param name="secondSpan">Second span of voice segment data.</param>
        public VoiceSegment(ReadOnlySpan<float> firstSpan, ReadOnlySpan<float> secondSpan)
        {
            this.length = firstSpan.Length + secondSpan.Length;

            this.buffer = ArrayPool<float>.Shared.Rent(this.length);
            firstSpan.CopyTo(this.buffer.AsSpan(0..firstSpan.Length));
            secondSpan.CopyTo(this.buffer.AsSpan(firstSpan.Length..this.length));

            this.volume = CalculateVolume(this.buffer, this.length);
        }

        public void Dispose()
        {
            ArrayPool<float>.Shared.Return(buffer, clearArray: false);
        }

        /// <summary>
        /// Calculates the volume (root mean square) of this voice segment.
        /// </summary>
        /// <returns></returns>
        private static float CalculateVolume(float[] data, int length)
        {
            var sum = 0f;
            for (var i = 0; i < length; i++)
            {
                var sample = data[i];
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
            return new VoiceSegment(this.buffer.AsSpan(start: 0, length));
        }
    }
}
