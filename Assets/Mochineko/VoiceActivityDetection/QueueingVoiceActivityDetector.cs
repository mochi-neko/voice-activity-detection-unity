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
        private readonly float activationIntervalSeconds;
        private readonly float deactivationIntervalSeconds;
        private readonly float maxDurationSeconds;

        private readonly CompositeDisposable compositeDisposable = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Stopwatch intervalStopwatch = new();
        private readonly Stopwatch totalDurationStopwatch = new();

        private readonly ReactiveProperty<bool> isActive = new();
        public IReadOnlyReactiveProperty<bool> IsActive => isActive;

        public QueueingVoiceActivityDetector(
            IVoiceSource source,
            IVoiceBuffer buffer,
            float maxQueueingTimeSeconds,
            float activeVolumeThreshold,
            float activationRateThreshold,
            float deactivationRateThreshold,
            float activationIntervalSeconds,
            float deactivationIntervalSeconds,
            float maxDurationSeconds)
        {
            this.source = source;
            this.buffer = buffer;
            this.queue = new VoiceSegmentActivityQueue(maxQueueingTimeSeconds);
            this.activeVolumeThreshold = activeVolumeThreshold;
            this.activationRateThreshold = activationRateThreshold;
            this.deactivationRateThreshold = deactivationRateThreshold;
            this.activationIntervalSeconds = activationIntervalSeconds;
            this.deactivationIntervalSeconds = deactivationIntervalSeconds;
            this.maxDurationSeconds = maxDurationSeconds;

            this.source
                .OnSegmentRead
                .Subscribe(OnSegmentReadAsync)
                .AddTo(compositeDisposable);

            this.intervalStopwatch.Start();
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Dispose();
            this.compositeDisposable.Dispose();
            this.buffer.Dispose();
            this.source.Dispose();
            this.intervalStopwatch.Stop();
            this.totalDurationStopwatch.Stop();
        }

        public void Update()
        {
            this.source.Update();
        }

        private async void OnSegmentReadAsync(VoiceSegment segment)
        {
            var volume = segment.Volume();
            Log.Verbose("[VAD] Volume: {0}.", volume.ToString("F4"));

            var isActiveSegment = volume >= activeVolumeThreshold;
            Log.Verbose("[VAD] Is active segment: {0}.", isActiveSegment);

            queue.Enqueue(new VoiceSegmentActivity(
                isActiveSegment,
                length: segment.length,
                this.source.SamplingRate,
                this.source.Channels)
            );

            var activeRate = queue.ActiveTimeRate();
            Log.Verbose("[VAD] Active rate in queue: {0}.", activeRate);

            if (!isActive.Value
                && activeRate >= activationRateThreshold
                && intervalStopwatch.ElapsedMilliseconds >= activationIntervalSeconds * 1000)
            {
                Log.Debug("[VAD] Activated.");
                await this.buffer.OnActiveAsync(this.cancellationTokenSource.Token);
                this.isActive.Value = true;
                intervalStopwatch.Restart();
                totalDurationStopwatch.Restart();
            }
            else if (
                isActive.Value
                && (totalDurationStopwatch.ElapsedMilliseconds >= maxDurationSeconds * 1000
                    || (activeRate <= deactivationRateThreshold
                        && intervalStopwatch.ElapsedMilliseconds >= deactivationIntervalSeconds * 1000)))
            {
                Log.Debug("[VAD] Deactivated.");
                await this.buffer.OnInactiveAsync(this.cancellationTokenSource.Token);
                this.isActive.Value = false;
                intervalStopwatch.Restart();
                return;
            }

            if (isActive.Value)
            {
                await this.buffer.BufferAsync(segment, this.cancellationTokenSource.Token);
            }
        }
    }
}