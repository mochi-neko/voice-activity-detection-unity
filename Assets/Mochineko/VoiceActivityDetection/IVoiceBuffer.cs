#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Buffers voice data.
    /// </summary>
    public interface IVoiceBuffer : IDisposable
    {
        /// <summary>
        /// Buffers voice segment.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask BufferAsync(VoiceSegment segment, CancellationToken cancellationToken);
    }
}