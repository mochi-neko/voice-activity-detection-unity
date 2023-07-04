#nullable enable
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A voice buffer that writes to a wave file stream.
    /// </summary>
    public sealed class WaveVoiceBuffer : IVoiceBuffer
    {
        private readonly IWaveStreamReceiver receiver;
        private readonly WaveFormat format;

        private readonly object lockObject = new();
        
        private Stream? stream;
        private WaveFileWriter? writer;

        public WaveVoiceBuffer(
            IWaveStreamReceiver receiver,
            int samplingRate = 44100,
            int bitsPerSample = 16,
            int channels = 1)
        {
            this.receiver = receiver;
            this.format = new WaveFormat(
                rate: samplingRate,
                bits: bitsPerSample,
                channels: channels);
        }

        public void Dispose()
        {
            Reset();
        }

        public async UniTask BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (writer == null)
            {
                throw new InvalidOperationException();
            }

            await UniTask.SwitchToThreadPool();

            lock (lockObject)
            {
                writer.WriteSamples(segment.buffer, offset: 0, segment.length);
            }

            await UniTask.SwitchToMainThread(cancellationToken);
        }

        public UniTask OnActiveAsync(CancellationToken cancellationToken)
        {
            Reset();

            stream = new MemoryStream();

            lock (lockObject)
            {
                writer = new WaveFileWriter(
                    outStream: stream,
                    format: format);
            }

            return UniTask.CompletedTask;
        }

        public async UniTask OnInactiveAsync(CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new InvalidOperationException();
            }

            if (writer == null)
            {
                throw new InvalidOperationException();
            }

            await writer.FlushAsync(cancellationToken);

            // NOTE: Please dispose stream by receiver.
            var copiedStream = new MemoryStream();
            stream.Seek(offset: 0, SeekOrigin.Begin);
            await stream.CopyToAsync(copiedStream, cancellationToken);
            copiedStream.Seek(offset: 0, SeekOrigin.Begin);

            receiver.OnReceive(copiedStream);
        }

        private void Reset()
        {
            lock (lockObject)
            {
                writer?.Dispose();
                writer = null;
                
                // NOTE: stream is disposed by writer.
                stream = null;
            }
        }
    }
}