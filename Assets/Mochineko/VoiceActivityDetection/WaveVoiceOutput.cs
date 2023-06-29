#nullable enable
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class WaveVoiceOutput : IVoiceOutput
    {
        private readonly WaveFileWriter writer;
        private int offset;
        
        public WaveVoiceOutput(
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
        
        public async UniTask WriteAsync(float[] buffer, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();
            
            writer.WriteSamples(buffer, offset, count);
            offset += count;

            await UniTask.SwitchToMainThread(cancellationToken);
        }
    }
}