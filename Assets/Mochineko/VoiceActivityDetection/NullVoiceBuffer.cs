#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A null object of <see cref="IVoiceBuffer"/> that do nothing.
    /// </summary>
    public sealed class NullVoiceBuffer : IVoiceBuffer
    {
        UniTask IVoiceBuffer.BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
            => UniTask.CompletedTask;

        UniTask IVoiceBuffer.OnActiveAsync(CancellationToken cancellationToken)
            => UniTask.CompletedTask;

        UniTask IVoiceBuffer.OnInactiveAsync(CancellationToken cancellationToken)
            => UniTask.CompletedTask;
        
        void IDisposable.Dispose()
        {
        }
    }
}