#nullable enable
using System;
using System.Collections.Concurrent;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A queue of <see cref="VoiceSegment"/>.
    /// </summary>
    internal sealed class VoiceSegmentQueue
    {
        private readonly ConcurrentQueue<VoiceSegment> queue = new();
        private readonly int maxQueueingLength;

        private int currentLength = 0;

        /// <summary>
        ///
        /// </summary>
        /// <param name="maxQueueingTimeSeconds"></param>
        /// <param name="frequency"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public VoiceSegmentQueue(float maxQueueingTimeSeconds, int frequency)
        {
            if (maxQueueingTimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxQueueingTimeSeconds), maxQueueingTimeSeconds, "maxQueueingTimeSeconds must be positive value.");
            }

            if (frequency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "frequency must be positive value.");
            }

            this.maxQueueingLength = (int)(maxQueueingTimeSeconds * frequency);
        }

        public void Enqueue(VoiceSegment segment)
        {
            queue.Enqueue(segment);
            currentLength += segment.length;

            while (currentLength > maxQueueingLength)
            {
                queue.TryDequeue(out var dequeued);
                currentLength -= dequeued.length;
            }
        }

        public bool TryDequeue(out VoiceSegment segment)
        {
            var result = queue.TryDequeue(out segment);
            if (result)
            {
                currentLength -= segment.length;
            }

            return result;
        }

        public void Clear()
        {
            queue.Clear();
            currentLength = 0;
        }
    }
}
