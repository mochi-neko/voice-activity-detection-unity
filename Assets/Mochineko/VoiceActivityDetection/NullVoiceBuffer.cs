#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class NullVoiceBuffer : IVoiceBuffer
    {
        public void Dispose()
        {
        }

        public UniTask BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}