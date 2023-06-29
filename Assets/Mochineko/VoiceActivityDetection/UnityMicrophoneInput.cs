#nullable enable
using System;
using UniRx;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class UnityMicrophoneInput : IVoiceInput
    {
        private readonly UnityMicrophoneProxy proxy;
        private readonly AudioClip audioClip;
        private readonly float[] loopBuffer;
        private readonly float[] readBuffer;
        private int currentPosition;
        private int lastPosition;

        private readonly Subject<(float[] buffer, int lenght)> onBufferRead = new();
        public IObservable<(float[] buffer, int lenght)> OnBufferRead => onBufferRead;

        public UnityMicrophoneInput(
            string? deviceName = null,
            int loopLengthSeconds = 1, // loopLength must be greater than update interval
            int frequency = 44100,
            int readBufferSize = 1024)
        {
            this.proxy = new UnityMicrophoneProxy(deviceName, loopLengthSeconds, frequency);
            this.audioClip = this.proxy.AudioClip;
            this.loopBuffer = new float[loopLengthSeconds * frequency];
            this.readBuffer = new float[readBufferSize];
        }

        public void Update()
        {
            currentPosition = this.proxy.GetSamplePosition();
            if (currentPosition <= 0)
            {
                lastPosition = currentPosition;
                return;
            }
            
            // Write current data to loop buffer
            this.audioClip.GetData(this.loopBuffer, offsetSamples:0);
            
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
                    Array.Clear(this.readBuffer, index:0, this.readBuffer.Length);
            
                    var readLength = Math.Min(this.readBuffer.Length, length - offset);
                    var slice = span.Slice(offset, readLength);
                    slice.CopyTo(this.readBuffer);
            
                    onBufferRead.OnNext((this.readBuffer, readLength));
            
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
                    Array.Clear(this.readBuffer, index:0, this.readBuffer.Length);
            
                    var readLength = Math.Min(this.readBuffer.Length, length - offset);
                    if (offset < toEnd.Length)
                    {
                        if (offset + readLength <= toEnd.Length) // Read all from toEnd
                        {
                            var slice = toEnd.Slice(offset, readLength);
                            slice.CopyTo(this.readBuffer);
                        }
                        else // Read from toEnd and fromStart
                        {
                            var sliceInToEnd = toEnd.Slice(offset, readLength);
                            sliceInToEnd.CopyTo(this.readBuffer.AsSpan(0..sliceInToEnd.Length));
                            
                            var sliceInFromStart = fromStart.Slice(start:0, readLength - sliceInToEnd.Length);
                            sliceInFromStart.CopyTo(this.readBuffer.AsSpan(sliceInToEnd.Length..readLength));
                        }
                    }
                    else // Read all from fromStart
                    {
                        var slice = fromStart.Slice(offset - toEnd.Length, readLength);
                        slice.CopyTo(this.readBuffer);
                    }

                    onBufferRead.OnNext((this.readBuffer, readLength));
            
                    offset += readLength;
                }
            }
        }
    }
}