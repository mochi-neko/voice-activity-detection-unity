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
        private readonly VoiceSegmentActivityQueue activityQueue;
        private readonly VoiceSegmentQueue activationQueue;
        private readonly float minQueueingTimeSeconds;
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

        private readonly ReactiveProperty<bool> voiceIsActive = new();
        IReadOnlyReactiveProperty<bool> IVoiceActivityDetector.VoiceIsActive => voiceIsActive;

        /// <summary>
        /// Create a new instance of <see cref="QueueingVoiceActivityDetector"/>.
        /// </summary>
        /// <param name="source">Source of voice data.</param>
        /// <param name="buffer">Buffer of voice data.</param>
        /// <param name="maxQueueingTimeSeconds">Max time(sec) to queue voice segment.</param>
        /// <param name="minQueueingTimeSeconds">Min time(sec) to queue voice segment to detect.</param>
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
            float minQueueingTimeSeconds,
            float activeVolumeThreshold,
            float activationRateThreshold,
            float inactivationRateThreshold,
            float activationIntervalSeconds,
            float inactivationIntervalSeconds,
            float maxActiveDurationSeconds)
        {
            this.source = source;
            this.buffer = buffer;
            this.activityQueue = new VoiceSegmentActivityQueue(maxQueueingTimeSeconds);
            this.activationQueue = new VoiceSegmentQueue(maxQueueingTimeSeconds, this.source.SamplingRate);
            this.minQueueingTimeSeconds = minQueueingTimeSeconds;
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
            this.voiceIsActive.Dispose();
        }

        void IVoiceActivityDetector.Update()
        {
            this.source.Update();
        }

        void IVoiceActivityDetector.SetDetectorActive(bool isActive)
        {
            this.source.SetSourceActive(isActive);

            if (!isActive)
            {
                this.activationQueue.Clear();
                this.activityQueue.Clear();
            }
        }

        private async void OnSegmentReadAsync(VoiceSegment segment)
        {
            var volume = segment.Volume();
            Log.Verbose("[VAD] Volume: {0}.", volume.ToString("F4"));

            var isActiveSegment = volume >= activeVolumeThreshold;
            Log.Verbose("[VAD] Is active segment: {0}.", isActiveSegment);

            activityQueue.Enqueue(new VoiceSegmentActivity(
                isActiveSegment,
                length: segment.length,
                this.source.SamplingRate,
                this.source.Channels)
            );

            // NOTE: Remove initial noise when queue length is short.
            if (activityQueue.TotalTimeSeconds < minQueueingTimeSeconds)
            {
                return;
            }

            var activeRate = activityQueue.ActiveTimeRate();

            // Change to active
            if (!voiceIsActive.Value
                && activeRate >= activationRateThreshold
                && intervalStopwatch.ElapsedMilliseconds >= activationIntervalSeconds * 1000)
            {
                Log.Info("[VAD] Activated.");
                await this.buffer.OnVoiceActiveAsync(this.cancellationTokenSource.Token);

                // Write buffers of segments that are buffered while inactive state just before activation.
                while (activationQueue.TryDequeue(out var queued) &&
                       !cancellationTokenSource.IsCancellationRequested)
                {
                    await this.buffer.BufferAsync(queued, cancellationTokenSource.Token);
                }

                this.voiceIsActive.Value = true;
                intervalStopwatch.Restart();
                totalDurationStopwatch.Restart();
                return;
            }
            // Change to inactive
            else if (
                voiceIsActive.Value
                && (totalDurationStopwatch.ElapsedMilliseconds >= maxActiveDurationSeconds * 1000
                    || (activeRate <= inactivationRateThreshold
                        && intervalStopwatch.ElapsedMilliseconds >= inactivationIntervalSeconds * 1000)))
            {
                Log.Info("[VAD] Inactivated.");
                await this.buffer.OnVoiceInactiveAsync(this.cancellationTokenSource.Token);
                this.voiceIsActive.Value = false;
                intervalStopwatch.Restart();
                return;
            }

            if (voiceIsActive.Value)
            {
                await this.buffer.BufferAsync(segment, this.cancellationTokenSource.Token);
            }
            else
            {
                // NOTE: Copy segment data because buffer array is reused.
                this.activationQueue.Enqueue(segment.Copy());
            }
        }
    }
}
