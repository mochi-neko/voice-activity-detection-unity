#nullable enable
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;
using UniRx;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A voice buffer that writes to a wave file stream.
    /// </summary>
    public sealed class WaveVoiceBuffer : IVoiceBuffer
    {
        private readonly WaveFormat format;
        private readonly bool disposeStream;
        
        private Stream? stream;
        private WaveFileWriter? writer;
        private int offset;

        private readonly Subject<Stream> onBufferCompleted = new();
        public IObservable<Stream> OnBufferCompleted => onBufferCompleted;

        public WaveVoiceBuffer(
            int samplingRate = 44100,
            int bitsPerSample = 16,
            int channels = 1,
            bool disposeStream = true)
        {
            this.format = new WaveFormat(
                rate: samplingRate,
                bits: bitsPerSample,
                channels: channels);

            this.disposeStream = disposeStream;
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
            
            writer.WriteSamples(segment.buffer, offset:0, segment.length);
            offset += segment.length;

            await UniTask.SwitchToMainThread(cancellationToken);
        }
        
        public void OnActive()
        {
            Reset();
            
            stream = new MemoryStream();
            
            writer = new WaveFileWriter(
                outStream: new IgnoreDisposeStream(stream),
                format: format);
        }

        public void OnInactive()
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset: 0, origin: SeekOrigin.Begin);
            onBufferCompleted.OnNext(stream);
        }
        
        private void Reset()
        {
            writer?.Dispose();
            if (disposeStream)
            {
                stream?.Dispose();
            }

            writer = null;
            stream = null;
            offset = 0;    
        }
    }
}