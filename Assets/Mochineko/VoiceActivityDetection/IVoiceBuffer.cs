#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    public interface IVoiceBuffer : IDisposable
    {
        UniTask BufferAsync(VoiceSegment segment, CancellationToken cancellationToken);
    }
}