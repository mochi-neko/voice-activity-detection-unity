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
        {
            return UniTask.CompletedTask;
        }
        
        public void OnActive()
        {
        }
        
        public void OnInactive()
        {
        }
    }
}