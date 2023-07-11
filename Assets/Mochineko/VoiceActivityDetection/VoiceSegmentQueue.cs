#nullable enable
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
        
        public VoiceSegmentQueue(float maxQueueingTimeSeconds, int frequency)
        {
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
    }
}