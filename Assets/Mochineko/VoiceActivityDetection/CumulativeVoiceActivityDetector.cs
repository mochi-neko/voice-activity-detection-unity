#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Logging;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class CumulativeVoiceActivityDetector : IVoiceActivityDetector
    {
        private readonly IVoiceSource source;
        private readonly IVoiceBuffer buffer;

        private readonly float activeVolumeThreshold;
        private readonly float activeChargeTimeRate;
        private readonly float maxChargeTimeSeconds;
        private readonly float minCumulatedTimeSeconds;
        private readonly float maxCumulatedTimeSeconds;

        private readonly ActiveState activeState;
        private readonly InactivateState inactivateState;
        private readonly IDisposable onSegmentReadDisposable;
        private readonly CancellationTokenSource cancellationTokenSource = new();

        private readonly ReactiveProperty<bool> voiceIsActive = new();
        IReadOnlyReactiveProperty<bool> IVoiceActivityDetector.VoiceIsActive => voiceIsActive;

        public CumulativeVoiceActivityDetector(
            IVoiceSource source,
            IVoiceBuffer buffer,
            float activeVolumeThreshold,
            float activeChargeTimeRate,
            float maxChargeTimeSeconds,
            float minCumulatedTimeSeconds,
            float maxCumulatedTimeSeconds)
        {
            if (activeVolumeThreshold <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(activeVolumeThreshold), activeVolumeThreshold,
                    "Must be greater than 0.");
            }

            if (activeChargeTimeRate <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(activeChargeTimeRate), activeChargeTimeRate,
                    "Must be greater than 0.");
            }

            if (maxChargeTimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxChargeTimeSeconds), maxChargeTimeSeconds,
                    "Must be greater than 0.");
            }

            if (minCumulatedTimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minCumulatedTimeSeconds),
                    minCumulatedTimeSeconds, "Must be greater than 0.");
            }

            if (maxCumulatedTimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCumulatedTimeSeconds),
                    maxCumulatedTimeSeconds, "Must be greater than 0.");
            }

            this.source = source;
            this.buffer = buffer;

            this.activeVolumeThreshold = activeVolumeThreshold;
            this.activeChargeTimeRate = activeChargeTimeRate;
            this.maxChargeTimeSeconds = maxChargeTimeSeconds;
            this.minCumulatedTimeSeconds = minCumulatedTimeSeconds;
            this.maxCumulatedTimeSeconds = maxCumulatedTimeSeconds;

            onSegmentReadDisposable = this.source
                .OnSegmentRead
                .Subscribe(OnSegmentReadAsync);

            this.activeState = new ActiveState(this);
            this.inactivateState = new InactivateState(this);

            inactivateState.Enter();
        }

        void IDisposable.Dispose()
        {
            onSegmentReadDisposable.Dispose();
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            activeState.Exit();
            inactivateState.Exit();
            buffer.Dispose();
            source.Dispose();
        }

        void IVoiceActivityDetector.SetDetectorActive(bool isActive)
        {
            source.SetSourceActive(isActive);

            // Force to inactivate
            if (!isActive && voiceIsActive.Value)
            {
                activeState.Exit();
                inactivateState.Enter();

                buffer.OnVoiceInactiveAsync(cancellationTokenSource.Token)
                    .Forget();
            }
        }

        void IVoiceActivityDetector.Update()
        {
            source.Update();
        }

        private async void OnSegmentReadAsync(VoiceSegment segment)
        {
            var cancellationToken = cancellationTokenSource.Token;

            if (voiceIsActive.Value)
            {
                var changeState = await activeState.UpdateAsync(segment, cancellationToken);
                if (changeState)
                {
                    // Change to InactivateState
                    activeState.Exit();
                    inactivateState.Enter();

                    await buffer.OnVoiceInactiveAsync(cancellationToken);
                }
            }
            else
            {
                var changeState = await inactivateState.UpdateAsync(segment, cancellationToken);
                if (changeState)
                {
                    // Change to ActiveState
                    inactivateState.Exit();
                    activeState.Enter();

                    await buffer.OnVoiceActiveAsync(cancellationToken);

                    // NOTE: Add initial segment to queue in active state.
                    var __ = await activeState.UpdateAsync(segment, cancellationToken);
                }
            }
        }

        private sealed class ActiveState
        {
            private readonly CumulativeVoiceActivityDetector parent;
            private readonly ConcurrentQueue<VoiceSegment> queue = new();

            private float chargeTimeSeconds;
            private float cumulatedTimeSeconds;
            private float activeTimeSeconds;

            public ActiveState(CumulativeVoiceActivityDetector parent)
            {
                this.parent = parent;
            }

            public void Enter()
            {
                Log.Debug("[VAD] Enter ActiveState.");
                parent.voiceIsActive.Value = true;
                chargeTimeSeconds = parent.maxChargeTimeSeconds;
                cumulatedTimeSeconds = 0f;
                activeTimeSeconds = 0f;
            }

            public void Exit()
            {
                while (queue.TryDequeue(out var segment))
                {
                   segment.Dispose();
                }
            }

            public async UniTask<bool> UpdateAsync(
                VoiceSegment segment,
                CancellationToken cancellationToken)
            {
                var isActive = segment.Volume >= parent.activeVolumeThreshold;
                var durationSeconds = segment.DurationSeconds;
                cumulatedTimeSeconds += durationSeconds;

                queue.Enqueue(segment);

                // Spend
                chargeTimeSeconds -= durationSeconds;
                if (isActive)
                {
                    activeTimeSeconds += durationSeconds;
                    // Charge
                    chargeTimeSeconds += durationSeconds * parent.activeChargeTimeRate;
                }

                if (chargeTimeSeconds > parent.maxChargeTimeSeconds)
                {
                    // Limit
                    chargeTimeSeconds = parent.maxChargeTimeSeconds;
                }

                Log.Debug("[VAD] Charge time: {0}, Cumulated time: {1}", chargeTimeSeconds, cumulatedTimeSeconds);

                // Finish active state
                if (cumulatedTimeSeconds >= parent.maxCumulatedTimeSeconds
                    || chargeTimeSeconds <= 0f)
                {
                    var isEffectiveSegments = activeTimeSeconds >= parent.minCumulatedTimeSeconds;
                    if (isEffectiveSegments)
                    {
                        Log.Debug("[VAD] Effective segments: {0}", activeTimeSeconds);
                        // Write all segments in queue to buffer.
                        while (
                            queue.TryDequeue(out var dequeued)
                            && !cancellationToken.IsCancellationRequested)
                        {
                            await parent.buffer.BufferAsync(dequeued, cancellationToken);
                            dequeued.Dispose();
                        }
                    }
                    else
                    {
                        // NOTE: Not effective segments are ignored.
                        Log.Debug("[VAD] Ignored segments: {0}", activeTimeSeconds);
                    }

                    // Change to InactivateState
                    return true;
                }
                else
                {
                    // Stay ActiveState
                    return false;
                }
            }
        }

        private sealed class InactivateState
        {
            private readonly CumulativeVoiceActivityDetector parent;

            public InactivateState(CumulativeVoiceActivityDetector parent)
            {
                this.parent = parent;
            }

            public void Enter()
            {
                Log.Debug("[VAD] Enter InactiveState.");
                parent.voiceIsActive.Value = false;
            }

            public void Exit()
            {
            }

            public UniTask<bool> UpdateAsync(
                VoiceSegment segment,
                CancellationToken cancellationToken)
            {
                var isActive = segment.Volume >= parent.activeVolumeThreshold;
                if (isActive)
                {
                    // Change to ActiveState
                    // NOTE: Not dispose segment to enqueue in ActiveState.
                    return UniTask.FromResult(true);
                }
                else
                {
                    // Stay InactivateState
                    segment.Dispose();
                    return UniTask.FromResult(false);
                }
            }
        }
    }
}
