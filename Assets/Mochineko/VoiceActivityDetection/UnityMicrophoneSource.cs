#nullable enable
using System;
using UniRx;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UnityMicrophoneSource : IVoiceSource
    {
        private readonly UnityMicrophoneProxy proxy;
        private readonly AudioClip audioClip;
        private readonly float[] loopBuffer;
        private readonly float[] readBuffer;
        private readonly int frequency;
        private int currentPosition;
        private int lastPosition;

        private readonly Subject<VoiceSegment> onSegmentRead = new();
        public IObservable<VoiceSegment> OnSegmentRead => onSegmentRead;
        
        int IVoiceSource.SamplingRate => frequency;
        int IVoiceSource.Channels => 1;

        public UnityMicrophoneSource(
            string? deviceName = null,
            int loopLengthSeconds = 1, // loopLength must be greater than update interval
            int frequency = 44100,
            int readBufferSize = 1024)
        {
            this.proxy = new UnityMicrophoneProxy(deviceName, loopLengthSeconds, frequency);
            this.audioClip = this.proxy.AudioClip;
            this.loopBuffer = new float[loopLengthSeconds * frequency];
            this.readBuffer = new float[readBufferSize];
            this.frequency = frequency;
        }

        public void Update()
        {
            currentPosition = this.proxy.GetSamplePosition();
            if (currentPosition < 0)
            {
                lastPosition = currentPosition;
                return;
            }

            if (lastPosition < 0)
            {
                lastPosition = 0;
            }

            // Write current data to loop buffer
            this.audioClip.GetData(this.loopBuffer, offsetSamples: 0);

            // Read samples from last position to current position
            ReadCurrentSamples();

            // Update last position
            lastPosition = currentPosition;
        }

        public void Dispose()
        {
            this.proxy.Dispose();
        }

        private void ReadCurrentSamples()
        {
            var length = currentPosition - lastPosition;
            if (length > 0)
            {
                var span = this.loopBuffer.AsSpan(lastPosition..currentPosition);
                var offset = 0;
                while (offset < length)
                {
                    Array.Clear(this.readBuffer, index: 0, this.readBuffer.Length);

                    var readLength = Math.Min(this.readBuffer.Length, length - offset);
                    var slice = span.Slice(offset, readLength);
                    slice.CopyTo(this.readBuffer);

                    onSegmentRead.OnNext(new VoiceSegment(this.readBuffer, readLength));

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
                    Array.Clear(this.readBuffer, index: 0, this.readBuffer.Length);

                    var readLength = Math.Min(this.readBuffer.Length, length - offset);
                    // Read all from toEnd
                    if (offset < toEnd.Length - readLength)
                    {
                        var slice = toEnd.Slice(offset, readLength);
                        slice.CopyTo(this.readBuffer);
                    }
                    // Read from toEnd and fromStart
                    else if (offset < toEnd.Length)
                    {
                        var readLenghtFromToEnd = toEnd.Length - offset;
                        var sliceInToEnd = toEnd.Slice(offset, readLenghtFromToEnd);
                        sliceInToEnd.CopyTo(this.readBuffer.AsSpan(0..readLenghtFromToEnd));

                        var sliceInFromStart = fromStart.Slice(start: 0, readLength - readLenghtFromToEnd);
                        sliceInFromStart.CopyTo(this.readBuffer.AsSpan(readLenghtFromToEnd..readLength));
                    }
                    // Read all from fromStart
                    else
                    {
                        var slice = fromStart.Slice(offset - toEnd.Length, readLength);
                        slice.CopyTo(this.readBuffer);
                    }

                    onSegmentRead.OnNext(new VoiceSegment(this.readBuffer, readLength));

                    offset += readLength;
                }
            }
        }
    }
}