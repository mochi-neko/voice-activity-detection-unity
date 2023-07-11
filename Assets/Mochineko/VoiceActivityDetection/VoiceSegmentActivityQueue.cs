#nullable enable
using System.Collections.Concurrent;
using System.Linq;

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
        
        public VoiceSegmentActivityQueue(float maxQueueingTimeSeconds)
        {
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
    }
}