#nullable enable
using System;
using UniRx;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A voice source that uses UnityEngine.Microphone.
    /// </summary>
    public sealed class UnityMicrophoneSource : IVoiceSource
    {
        private readonly UnityMicrophoneProxy proxy;
        private readonly AudioClip audioClip;
        private readonly float[] loopBuffer;
        private readonly int readBufferSize;
        private readonly int frequency;
        private readonly int channels;
        private int currentPosition;
        private int lastPosition;
        private bool isActive = true;

        private readonly object lockObject = new();

        private readonly Subject<VoiceSegment> onSegmentRead = new();
        IObservable<VoiceSegment> IVoiceSource.OnSegmentRead => onSegmentRead;

        int IVoiceSource.SamplingRate => frequency;
        int IVoiceSource.Channels => channels;

        /// <summary>
        /// Creates a new instance of <see cref="UnityMicrophoneSource"/>.
        /// </summary>
        /// <param name="proxy">Proxy of UnityEngine.Microphone.</param>
        /// <param name="readBufferSize">Fixed buffer size to read voice data at once.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public UnityMicrophoneSource(
            UnityMicrophoneProxy proxy,
            int readBufferSize = 4096)
        {
            if (readBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(readBufferSize), readBufferSize,
                    "Read buffer size must be greater than 0.");
            }

            this.proxy = proxy;
            this.frequency = this.proxy.GetMaxFrequency();
            this.audioClip = this.proxy.AudioClip;
            this.channels = this.audioClip.channels;
            this.loopBuffer = new float[proxy.LoopLengthSeconds * frequency];
            this.readBufferSize = readBufferSize;
        }

        void IDisposable.Dispose()
        {
            this.onSegmentRead.Dispose();
        }

        void IVoiceSource.Update()
        {
            lock (lockObject)
            {
                currentPosition = this.proxy.GetSamplePosition();
                if (currentPosition < 0)
                {
                    lastPosition = 0;
                    return;
                }

                if (!isActive)
                {
                    return;
                }

                // No update of microphone audio
                if (currentPosition == lastPosition)
                {
                    return;
                }

                // Write current all data to loop buffer
                this.audioClip.GetData(this.loopBuffer, offsetSamples: 0);

                // Read samples from last position to current position
                ReadCurrentSamples();

                // Update last position
                lastPosition = currentPosition;
            }
        }

        void IVoiceSource.SetSourceActive(bool isActive)
        {
            this.isActive = isActive;
        }

        private void ReadCurrentSamples()
        {
            var length = currentPosition - lastPosition;
            if (length == 0)
            {
                return;
            }

            if (length > 0)
            {
                var span = this.loopBuffer.AsSpan(lastPosition..currentPosition);
                var offset = 0;
                while (offset < length)
                {
                    var readLength = Math.Min(readBufferSize, length - offset);
                    var slice = span.Slice(offset, readLength);

                    onSegmentRead.OnNext(new VoiceSegment(slice, frequency, channels));

                    offset += readLength;
                }
            }
            else // Looped
            {
                length = this.loopBuffer.Length - lastPosition + currentPosition;
                var toEnd = this.loopBuffer.AsSpan(lastPosition..this.loopBuffer.Length);
                var fromStart = this.loopBuffer.AsSpan(0..currentPosition);
                var offset = 0;
                while (offset < length)
                {
                    var readLength = Math.Min(readBufferSize, length - offset);
                    // Read all from toEnd
                    if (offset < toEnd.Length - readLength)
                    {
                        var slice = toEnd.Slice(offset, readLength);
                        onSegmentRead.OnNext(new VoiceSegment(slice, frequency, channels));
                    }
                    // Read from toEnd and fromStart
                    else if (offset < toEnd.Length)
                    {
                        var readLenghtFromToEnd = toEnd.Length - offset;
                        var sliceInToEnd = toEnd.Slice(offset, readLenghtFromToEnd);
                        var sliceInFromStart = fromStart.Slice(start: 0, readLength - readLenghtFromToEnd);

                        onSegmentRead.OnNext(new VoiceSegment(sliceInToEnd, sliceInFromStart, frequency, channels));
                    }
                    // Read all from fromStart
                    else
                    {
                        var slice = fromStart.Slice(offset - toEnd.Length, readLength);
                        onSegmentRead.OnNext(new VoiceSegment(slice, frequency, channels));
                    }

                    offset += readLength;
                }
            }
        }
    }
}
