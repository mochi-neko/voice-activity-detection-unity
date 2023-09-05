#nullable enable
using System;
using System.Buffers;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Voice segment data.
    /// </summary>
    public sealed class VoiceSegment : IDisposable
    {
        /// <summary>
        /// Pooled buffer array of voice segment.
        /// </summary>
        public float[] Buffer { get; }

        /// <summary>
        /// Effective length of buffer array.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Volume (root mean square) of this voice segment.
        /// </summary>
        public float Volume { get; }

        /// <summary>
        /// Duration of this voice segment in seconds.
        /// </summary>
        public float DurationSeconds { get; }

        private bool disposed = false;

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="volume"></param>
        /// <param name="durationSeconds"></param>
        private VoiceSegment(
            ReadOnlySpan<float> span,
            float volume,
            float durationSeconds)
        {
            this.Length = span.Length;
            this.Buffer = ArrayPool<float>.Shared.Rent(this.Length);
            span.CopyTo(this.Buffer.AsSpan(0..this.Length));

            this.Volume = volume;
            this.DurationSeconds = durationSeconds;
        }

        /// <summary>
        /// Creates a new instance of <see cref="VoiceSegment"/>.
        /// </summary>
        /// <param name="span">Span of voice segment data.</param>
        /// <param name="frequency">Frequency of voice data.</param>
        /// <param name="channels">Channels count of voice data.</param>
        public VoiceSegment(
            ReadOnlySpan<float> span,
            int frequency,
            int channels)
        {
            this.Length = span.Length;

            this.Buffer = ArrayPool<float>.Shared.Rent(this.Length);
            span.CopyTo(this.Buffer.AsSpan(0..this.Length));

            this.Volume = CalculateVolume(this.Buffer, this.Length);
            this.DurationSeconds = (float)this.Length / frequency / channels;
        }

        /// <summary>
        /// Creates a new instance of <see cref="VoiceSegment"/>.
        /// </summary>
        /// <param name="firstSpan">First span of voice segment data.</param>
        /// <param name="secondSpan">Second span of voice segment data.</param>
        /// <param name="frequency">Frequency of voice data.</param>
        /// <param name="channels">Channels count of voice data.</param>
        public VoiceSegment(
            ReadOnlySpan<float> firstSpan,
            ReadOnlySpan<float> secondSpan,
            int frequency,
            int channels)
        {
            this.Length = firstSpan.Length + secondSpan.Length;

            this.Buffer = ArrayPool<float>.Shared.Rent(this.Length);
            firstSpan.CopyTo(this.Buffer.AsSpan(0..firstSpan.Length));
            secondSpan.CopyTo(this.Buffer.AsSpan(firstSpan.Length..this.Length));

            this.Volume = CalculateVolume(this.Buffer, this.Length);
            this.DurationSeconds = (float)this.Length / frequency / channels;
        }

        public void Dispose()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(VoiceSegment));
            }

            ArrayPool<float>.Shared.Return(Buffer, clearArray: false);
            disposed = true;
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
            return new VoiceSegment(
                this.Buffer.AsSpan(0..this.Length),
                this.Volume,
                this.DurationSeconds);
        }
    }
}
