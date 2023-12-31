﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Composites multiple <see cref="Mochineko.VoiceActivityDetection.IVoiceBuffer"/>s.
    /// </summary>
    public sealed class CompositeVoiceBuffer : IVoiceBuffer
    {
        private readonly IEnumerable<IVoiceBuffer> buffers;

        /// <summary>
        /// Create a new instance of <see cref="Mochineko.VoiceActivityDetection.CompositeVoiceBuffer"/>.
        /// </summary>
        /// <param name="buffers">Composited voice buffers.</param>
        public CompositeVoiceBuffer(params IVoiceBuffer[] buffers)
        {
            this.buffers = buffers;
        }

        async UniTask IVoiceBuffer.BufferAsync(VoiceSegment segment, CancellationToken cancellationToken)
        {
            foreach (var buffer in buffers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await buffer.BufferAsync(segment, cancellationToken);
            }
        }

        async UniTask IVoiceBuffer.OnVoiceActiveAsync(CancellationToken cancellationToken)
        {
            foreach (var buffer in buffers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await buffer.OnVoiceActiveAsync(cancellationToken);
            }
        }

        async UniTask IVoiceBuffer.OnVoiceInactiveAsync(CancellationToken cancellationToken)
        {
            foreach (var buffer in buffers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await buffer.OnVoiceInactiveAsync(cancellationToken);
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }
        }
    }
}
