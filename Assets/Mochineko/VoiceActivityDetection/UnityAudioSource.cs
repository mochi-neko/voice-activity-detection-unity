#nullable enable
using System;
using UniRx;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A voice source that uses UnityEngine.AudioSource via OnAudioFilterRead().
    /// </summary>
    public sealed class UnityAudioSource : IVoiceSource
    {
        private readonly int readBufferSize;
        private readonly bool mute;
        private readonly int samplingRate;

        private int channels = 1;
        private bool isActive = true;

        private readonly Subject<VoiceSegment> onSegmentRead = new();
        IObservable<VoiceSegment> IVoiceSource.OnSegmentRead => onSegmentRead;

        /// <summary>
        /// Creates a new instance of <see cref="UnityAudioSource"/>.
        /// </summary>
        /// <param name="readBufferSize">Fixed buffer size to read voice data at once.</param>
        /// <param name="mute">Mute output audio or not.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public UnityAudioSource(
            int readBufferSize = 4096,
            bool mute = false)
        {
            if (readBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(readBufferSize), readBufferSize, "Read buffer size must be greater than 0.");
            }

            this.readBufferSize = readBufferSize;
            this.mute = mute;
            this.samplingRate = UnityEngine.AudioSettings.outputSampleRate;
        }

        void IDisposable.Dispose()
        {
            onSegmentRead.Dispose();
        }

        int IVoiceSource.SamplingRate
            => this.samplingRate;

        int IVoiceSource.Channels
            => this.channels;

        void IVoiceSource.Update()
        {
            // Do nothing
        }

        void IVoiceSource.SetSourceActive(bool isActive)
        {
            this.isActive = isActive;
        }

        /// <summary>
        /// Redirects OnAudioFilterRead() to OnSegmentRead.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="channels"></param>
        public void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isActive)
            {
                return;
            }

            this.channels = channels;

            // Read data with buffer
            var position = 0;
            while (position < data.Length)
            {
                var readLength = Math.Min(readBufferSize, data.Length - position);
                var span = data.AsSpan(position, readLength);
                onSegmentRead.OnNext(new VoiceSegment(span, samplingRate, channels));
                position += readLength;
            }

            if (mute)
            {
                Array.Clear(data, 0, data.Length);
            }
        }
    }
}
