#nullable enable
using System;
using UniRx;
using Unity.Logging;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A voice source that uses UnityEngine.AudioSource via OnAudioFilterRead().
    /// </summary>
    public sealed class UnityAudioSource : IVoiceSource
    {
        private readonly float[] readBuffer;
        private readonly bool mute;
        private readonly int samplingRate;

        private int channels;
        private bool isActive = true;

        private readonly Subject<VoiceSegment> onSegmentRead = new();
        IObservable<VoiceSegment> IVoiceSource.OnSegmentRead => onSegmentRead;

        public UnityAudioSource(
            int readBufferSize = 4096,
            bool mute = false)
        {
            this.readBuffer = new float[readBufferSize];
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
            => 1;

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

            // Read data with buffer
            var position = 0;
            while (position < data.Length)
            {
                Array.Clear(readBuffer, index: 0, readBuffer.Length);

                var readLength = Math.Min(readBuffer.Length, data.Length - position);

                Array.Copy(data, position, readBuffer, destinationIndex: 0, readLength);

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
