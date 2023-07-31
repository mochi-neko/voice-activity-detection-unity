#nullable enable
using System.Collections.Concurrent;
using System.Linq;
using System;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A queue of <see cref="VoiceSegmentActivity"/>.
    /// </summary>
    internal sealed class VoiceSegmentActivityQueue
    {
        private readonly ConcurrentQueue<VoiceSegmentActivity> queue = new();
        private readonly float maxQueueingTimeSeconds;

        public float TotalTimeSeconds { get; private set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="maxQueueingTimeSeconds"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public VoiceSegmentActivityQueue(float maxQueueingTimeSeconds)
        {
            if (maxQueueingTimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxQueueingTimeSeconds), maxQueueingTimeSeconds, "maxQueueingTimeSeconds must be positive value.");
            }

            this.maxQueueingTimeSeconds = maxQueueingTimeSeconds;
        }

        public void Enqueue(VoiceSegmentActivity activity)
        {
            queue.Enqueue(activity);
            TotalTimeSeconds += activity.timeSeconds;

            while (TotalTimeSeconds > maxQueueingTimeSeconds)
            {
                queue.TryDequeue(out var dequeued);
                TotalTimeSeconds -= dequeued.timeSeconds;
            }
        }

        public float ActiveTimeRate()
        {
            if (!queue.Any())
            {
                return 0f;
            }

            var activeTimeSeconds = 0f;
            foreach (var activity in queue)
            {
                if (activity.isActive)
                {
                    activeTimeSeconds += activity.timeSeconds;
                }
            }

            return activeTimeSeconds / TotalTimeSeconds;
        }

        public void Clear()
        {
            queue.Clear();
            TotalTimeSeconds = 0f;
        }
    }
}
