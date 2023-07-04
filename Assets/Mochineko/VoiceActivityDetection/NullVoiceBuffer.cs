#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A null object of <see cref="IVoiceBuffer"/> that do nothing.
    /// </summary>
    public sealed class NullVoiceBuffer : IVoiceBuffer
    {
        public void Dispose()
        {
        }

        public UniTask BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
            => UniTask.CompletedTask;

        public UniTask OnActiveAsync(CancellationToken cancellationToken)
            => UniTask.CompletedTask;

        public UniTask OnInactiveAsync(CancellationToken cancellationToken)
            => UniTask.CompletedTask;
    }
}