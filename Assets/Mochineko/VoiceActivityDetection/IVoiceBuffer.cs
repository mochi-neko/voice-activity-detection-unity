#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Buffer of voice data.
    /// </summary>
    public interface IVoiceBuffer : IDisposable
    {
        /// <summary>
        /// Buffers voice segment.
        /// </summary>
        /// <param name="segment">Voice segment data to buffer.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        UniTask BufferAsync(VoiceSegment segment, CancellationToken cancellationToken);

        /// <summary>
        /// Called when voice has been active.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        UniTask OnVoiceActiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Called when voice has been inactive.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        UniTask OnVoiceInactiveAsync(CancellationToken cancellationToken);
    }
}
