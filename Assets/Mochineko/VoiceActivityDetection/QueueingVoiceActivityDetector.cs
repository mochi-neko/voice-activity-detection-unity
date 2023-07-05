#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using UniRx;
using Unity.Logging;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A simple implementation of <see cref="IVoiceActivityDetector"/>.
    /// Detects voice activity by using voice segment queue, volume threshold, activation/deactivation rate and interval. 
    /// </summary>
    public sealed class QueueingVoiceActivityDetector : IVoiceActivityDetector
    {
        private readonly IVoiceSource source;
        private readonly IVoiceBuffer buffer;
        private readonly VoiceSegmentActivityQueue queue;
        private readonly float activeVolumeThreshold;
        private readonly float activationRateThreshold;
        private readonly float inactivationRateThreshold;
        private readonly float activationIntervalSeconds;
        private readonly float inactivationIntervalSeconds;
        private readonly float maxActiveDurationSeconds;

        private readonly CompositeDisposable compositeDisposable = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Stopwatch intervalStopwatch = new();
        private readonly Stopwatch totalDurationStopwatch = new();

        private readonly ReactiveProperty<bool> isActive = new();
        IReadOnlyReactiveProperty<bool> IVoiceActivityDetector.IsActive => isActive;

        /// <summary>
        /// Create a new instance of <see cref="QueueingVoiceActivityDetector"/>.
        /// </summary>
        /// <param name="source">Source of voice data.</param>
        /// <param name="buffer">Buffer of voice data.</param>
        /// <param name="maxQueueingTimeSeconds">Max time(sec) to queue voice segment.</param>
        /// <param name="activeVolumeThreshold">Threshold of active voice volume by root mean square.</param>
        /// <param name="activationRateThreshold">Threshold of active rate in queue that changes into active state.</param>
        /// <param name="inactivationRateThreshold">Threshold of active rate in queue that changes into inactive state.</param>
        /// <param name="activationIntervalSeconds">Interval time(sec) to change from inactive state to active state.</param>
        /// <param name="inactivationIntervalSeconds">Interval time(sec) to change from active state to inactive state.</param>
        /// <param name="maxActiveDurationSeconds">Max time(sec) of active state.</param>
        public QueueingVoiceActivityDetector(
            IVoiceSource source,
            IVoiceBuffer buffer,
            float maxQueueingTimeSeconds,
            float activeVolumeThreshold,
            float activationRateThreshold,
            float inactivationRateThreshold,
            float activationIntervalSeconds,
            float inactivationIntervalSeconds,
            float maxActiveDurationSeconds)
        {
            this.source = source;
            this.buffer = buffer;
            this.queue = new VoiceSegmentActivityQueue(maxQueueingTimeSeconds);
            this.activeVolumeThreshold = activeVolumeThreshold;
            this.activationRateThreshold = activationRateThreshold;
            this.inactivationRateThreshold = inactivationRateThreshold;
            this.activationIntervalSeconds = activationIntervalSeconds;
            this.inactivationIntervalSeconds = inactivationIntervalSeconds;
            this.maxActiveDurationSeconds = maxActiveDurationSeconds;

            this.source
                .OnSegmentRead
                .Subscribe(OnSegmentReadAsync)
                .AddTo(compositeDisposable);

            this.intervalStopwatch.Start();
        }

        void IDisposable.Dispose()
        {
            this.cancellationTokenSource.Dispose();
            this.compositeDisposable.Dispose();
            this.buffer.Dispose();
            this.source.Dispose();
            this.intervalStopwatch.Stop();
            this.totalDurationStopwatch.Stop();
        }

        void IVoiceActivityDetector.Update()
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
                && (totalDurationStopwatch.ElapsedMilliseconds >= maxActiveDurationSeconds * 1000
                    || (activeRate <= inactivationRateThreshold
                        && intervalStopwatch.ElapsedMilliseconds >= inactivationIntervalSeconds * 1000)))
            {
                Log.Debug("[VAD] Inactivated.");
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