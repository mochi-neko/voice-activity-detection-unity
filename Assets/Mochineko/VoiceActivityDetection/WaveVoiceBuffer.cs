#nullable enable
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NAudio.Wave;
using Unity.Logging;

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

        private int sizeCounter;

        /// <summary>
        /// Creates a new instance of <see cref="WaveVoiceBuffer"/>.
        /// </summary>
        /// <param name="receiver">Receiver of wave file stream when voice changes inactive state.</param>
        /// <param name="samplingRate">Sampling rate of voice data.</param>
        /// <param name="bitsPerSample">Bits count per each sample to write wave data, 16, 24 or 32.</param>
        /// <param name="channels">Channels count of voice data</param>
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

        void IDisposable.Dispose()
        {
            Reset();
        }

        async UniTask IVoiceBuffer.BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (writer == null)
            {
                throw new InvalidOperationException();
            }

            await UniTask.SwitchToThreadPool();

            lock (lockObject)
            {
                sizeCounter += segment.length;
                Log.Verbose("[VAD] Write {0} / {1} samples to wave stream.", segment.length, sizeCounter);
                writer.WriteSamples(segment.buffer, offset: 0, segment.length);
            }

            await UniTask.SwitchToMainThread(cancellationToken);
        }

        UniTask IVoiceBuffer.OnActiveAsync(CancellationToken cancellationToken)
        {
            Reset();

            stream = new MemoryStream();

            lock (lockObject)
            {
                writer = new WaveFileWriter(
                    outStream: stream,
                    format: format);
            }

            sizeCounter = 0;

            return UniTask.CompletedTask;
        }

        async UniTask IVoiceBuffer.OnInactiveAsync(CancellationToken cancellationToken)
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