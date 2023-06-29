#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    public interface IVoiceOutput : IDisposable
    {
        UniTask WriteAsync(float[] buffer, int count, CancellationToken cancellationToken);
    }
}