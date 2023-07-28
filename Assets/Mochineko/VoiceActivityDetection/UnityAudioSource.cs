#nullable enable
using System;
using UniRx;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class UnityAudioSource : IVoiceSource
    {
        private readonly float[] readBuffer;
        private readonly bool mute;

        private int channels;
        private bool isActive;

        private readonly Subject<VoiceSegment> onSegmentRead = new();
        IObservable<VoiceSegment> IVoiceSource.OnSegmentRead => onSegmentRead;

        public UnityAudioSource(
            int readBufferSize = 4096,
            bool mute = false)
        {
            this.readBuffer = new float[readBufferSize];
            this.mute = mute;
        }

        void IDisposable.Dispose()
        {
            onSegmentRead.Dispose();
        }

        int IVoiceSource.SamplingRate
            => UnityEngine.AudioSettings.outputSampleRate;

        int IVoiceSource.Channels
            => channels;

        void IVoiceSource.Update()
        {
            // Do nothing
        }

        void IVoiceSource.SetSourceActive(bool isActive)
        {
            this.isActive = isActive;
        }

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
                Array.Clear(readBuffer, 0, readBuffer.Length);

                var readLength = Math.Min(readBuffer.Length, data.Length - position);

                Array.Copy(data, position, readBuffer, 0, readLength);

                onSegmentRead.OnNext(new VoiceSegment(readBuffer, readLength));

                position += readLength;
            }

            if (mute)
            {
                Array.Clear(data, 0, data.Length);
            }
        }
    }
}
