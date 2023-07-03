#nullable enable
using System.Diagnostics;
using System.Threading;
using UniRx;
using Unity.Logging;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A simple implementation of <see cref="IVoiceActivityDetector"/>.
    /// Detects voice activity by using voice segment queue, volume threshold, activation/deactivation rate and deactivation interval. 
    /// </summary>
    public sealed class QueueingVoiceActivityDetector : IVoiceActivityDetector
    {
        private readonly IVoiceSource source;
        private readonly IVoiceBuffer buffer;
        private readonly VoiceSegmentActivityQueue queue;
        private readonly float activeVolumeThreshold;
        private readonly float activationRateThreshold;
        private readonly float deactivationRateThreshold;
        private readonly float deactivationIntervalSeconds;

        private readonly CompositeDisposable compositeDisposable = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Stopwatch stopwatch = new();

        private readonly ReactiveProperty<bool> isActive = new();
        public IReadOnlyReactiveProperty<bool> IsActive => isActive;

        public QueueingVoiceActivityDetector(
            IVoiceSource source,
            IVoiceBuffer buffer,
            float maxQueueingTimeSeconds,
            float activeVolumeThreshold,
            float activationRateThreshold,
            float deactivationRateThreshold,
            float deactivationIntervalSeconds)
        {
            this.source = source;
            this.buffer = buffer;
            this.queue = new VoiceSegmentActivityQueue(maxQueueingTimeSeconds);
            this.activeVolumeThreshold = activeVolumeThreshold;
            this.activationRateThreshold = activationRateThreshold;
            this.deactivationRateThreshold = deactivationRateThreshold;
            this.deactivationIntervalSeconds = deactivationIntervalSeconds;

            this.source
                .OnSegmentRead
                .Subscribe(OnSegmentReadAsync)
                .AddTo(compositeDisposable);

            this.stopwatch.Start();
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Dispose();
            this.compositeDisposable.Dispose();
            this.buffer.Dispose();
            this.source.Dispose();
            this.stopwatch.Stop();
        }

        public void Update()
        {
            this.source.Update();
        }

        private async void OnSegmentReadAsync(
            VoiceSegment segment)
        {
            var volume = segment.Volume();
            Log.Verbose("[VAD] Volume: {0}.", volume.ToString("F4"));

            var isActiveSegment = volume >= activeVolumeThreshold;
            Log.Verbose("[VAD] Is active segment: {0}.", isActiveSegment);

            queue.Enqueue(new VoiceSegmentActivity(
                isActiveSegment,
                samplesCount: segment.length,
                this.source.SamplingRate,
                this.source.Channels)
            );

            var activeRate = queue.ActiveTimeRate();
            Log.Verbose("[VAD] Active rate in queue: {0}.", activeRate);

            if (!isActive.Value
                && activeRate >= activationRateThreshold)
            {
                Log.Debug("[VAD] Activated.");
                this.buffer.OnActive();
                this.isActive.Value = true;
                stopwatch.Restart();
            }
            else if (
                isActive.Value
                && activeRate <= deactivationRateThreshold
                && stopwatch.ElapsedMilliseconds >= deactivationIntervalSeconds * 1000)
            {
                Log.Debug("[VAD] Deactivated.");
                this.buffer.OnInactive();
                this.isActive.Value = false;
            }

            if (isActive.Value)
            {
                await this.buffer.BufferAsync(segment, this.cancellationTokenSource.Token);
            }
        }
    }
}