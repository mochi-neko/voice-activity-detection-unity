﻿#nullable enable
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

        private readonly Subject<AudioClip> onVoiceInactive = new();
        public IObservable<AudioClip> OnVoiceInactive => onVoiceInactive;

        /// <summary>
        /// Creates a new instance of <see cref="AudioClipBuffer"/>.
        /// </summary>
        /// <param name="maxSampleLength">Max sample length to record.</param>
        /// <param name="frequency">Frequency (= sampling rate) of voice data.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public AudioClipBuffer(int maxSampleLength, int frequency)
        {
            if (maxSampleLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSampleLength), maxSampleLength, "maxSampleLength must be positive value.");
            }

            if (frequency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "frequency must be positive value.");
            }

            this.audioClip = AudioClip.Create(
                name: "VAD_AudioClipBuffer",
                lengthSamples: maxSampleLength,
                channels: 1,
                frequency: frequency,
                stream: false);

            this.resetBuffer = new float[maxSampleLength];
        }

        async UniTask IVoiceBuffer.BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
        {
            await UniTask.SwitchToMainThread(cancellationToken);

            // NOTE: Copy to new buffer, namely allocated new array.
            var writeBuffer = segment.Buffer.AsSpan(0..segment.Length).ToArray();

            this.audioClip.SetData(writeBuffer, offsetSamples: position);

            position += segment.Length;
        }

        async UniTask IVoiceBuffer.OnVoiceActiveAsync(CancellationToken cancellationToken)
        {
            await UniTask.SwitchToMainThread(cancellationToken);

            this.audioClip.SetData(this.resetBuffer, offsetSamples: 0);

            position = 0;
        }

        UniTask IVoiceBuffer.OnVoiceInactiveAsync(CancellationToken cancellationToken)
        {
            onVoiceInactive.OnNext(this.audioClip);

            return UniTask.CompletedTask;
        }

        void IDisposable.Dispose()
        {
            Object.Destroy(this.audioClip);
        }
    }
}
