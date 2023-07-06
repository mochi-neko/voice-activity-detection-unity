#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A voice buffer to write to AudioClip.
    /// </summary>
    public sealed class AudioClipBuffer : IVoiceBuffer
    {
        private readonly AudioClip audioClip;
        private readonly float[] resetBuffer;

        private int position;

        private readonly Subject<AudioClip> onInactive = new();
        public IObservable<AudioClip> OnInactive => onInactive;

        /// <summary>
        /// Creates a new instance of <see cref="AudioClipBuffer"/>.
        /// </summary>
        /// <param name="maxSampleLength">Max sample length to record.</param>
        /// <param name="frequency">Frequency (= sampling rate) of voice data.</param>
        public AudioClipBuffer(int maxSampleLength, int frequency)
        {
            this.audioClip = AudioClip.Create(
                name: "VAD_AudioClipBuffer",
                lengthSamples: maxSampleLength,
                channels: 1,
                frequency: frequency,
                stream: false);

            this.resetBuffer = new float[maxSampleLength];
        }

        UniTask IVoiceBuffer.BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
        {
            // NOTE: Copy to new buffer, namely allocated new array.
            var writeBuffer = segment.buffer.AsSpan(0..segment.length).ToArray();

            this.audioClip.SetData(writeBuffer, offsetSamples: position);

            position += segment.length;

            return UniTask.CompletedTask;
        }

        UniTask IVoiceBuffer.OnActiveAsync(CancellationToken cancellationToken)
        {
            this.audioClip.SetData(this.resetBuffer, offsetSamples: 0);

            position = 0;

            return UniTask.CompletedTask;
        }

        UniTask IVoiceBuffer.OnInactiveAsync(CancellationToken cancellationToken)
        {
            onInactive.OnNext(this.audioClip);

            return UniTask.CompletedTask;
        }

        void IDisposable.Dispose()
        {
            Object.Destroy(this.audioClip);
        }
    }
}