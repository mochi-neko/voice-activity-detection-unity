#nullable enable
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A voice buffer that writes to a wave file.
    /// </summary>
    public sealed class WaveVoiceBuffer : IVoiceBuffer
    {
        private readonly WaveFileWriter writer;
        private int offset;
        
        public WaveVoiceBuffer(
            Stream output,
            bool ignoreDisposeStream = false,
            int samplingRate = 44100,
            int bitsPerSample = 16,
            int channels = 1)
        {
            var format = new WaveFormat(
                rate: samplingRate,
                bits: bitsPerSample,
                channels: channels);
            
            writer = new WaveFileWriter(
                ignoreDisposeStream ? new IgnoreDisposeStream(output) : output,
                format);
        }

        public void Dispose()
        {
            writer.Dispose();
        }
        
        public async UniTask BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();
            
            writer.WriteSamples(segment.buffer, offset, segment.length);
            offset += segment.length;

            await UniTask.SwitchToMainThread(cancellationToken);
        }
    }
}