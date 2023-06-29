#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class NullVoiceOutput : IVoiceOutput
    {
        public void Dispose()
        {
        }

        public UniTask WriteAsync(float[] buffer, int count, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}