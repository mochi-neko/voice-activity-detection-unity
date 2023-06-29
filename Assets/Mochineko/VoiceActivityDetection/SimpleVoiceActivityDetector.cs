#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A simple implementation of <see cref="IVoiceActivityDetector"/>.
    /// Detects activity by voice volume(root mean square) and false interval. 
    /// </summary>
    public sealed class SimpleVoiceActivityDetector : IVoiceActivityDetector
    {
        private readonly IVoiceSource source;
        private readonly IVoiceBuffer buffer;
        private readonly IDisposable disposable;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Stopwatch stopwatch = new();
        
        private readonly float volumeThreshold;
        private readonly float falseIntervalSeconds;

        private readonly ReactiveProperty<bool> isActive = new();
        public IReadOnlyReactiveProperty<bool> IsActive => isActive;

        public SimpleVoiceActivityDetector(
            IVoiceSource source,
            IVoiceBuffer buffer,
            float volumeThreshold,
            float falseIntervalSeconds)
        {
            this.source = source;
            this.buffer = buffer;
            this.volumeThreshold = volumeThreshold;
            this.falseIntervalSeconds = falseIntervalSeconds;

            disposable = this.source
                .OnSegmentRead
                .Subscribe(async value => await OnSegmentReadAsync(value));
            
            stopwatch.Start();
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
            disposable.Dispose();
            buffer.Dispose();
            source.Dispose();
        }

        public void Update()
        {
            source.Update();
        }

        private async UniTask OnSegmentReadAsync(VoiceSegment segment)
        {
            if (!IsActiveVoice(segment))
            {
                if (stopwatch.ElapsedMilliseconds > falseIntervalSeconds * 1000)
                {
                    stopwatch.Reset();
                    Log.Verbose("[VAD] Active: false");
                    isActive.Value = false;
                }
                return;
            }
            
            stopwatch.Restart();
            
            await this.buffer.BufferAsync(segment, cancellationTokenSource.Token);
            
            Log.Debug("[VAD] Active: true");
            isActive.Value = true;
        }

        private bool IsActiveVoice(VoiceSegment segment)
        {
            var volume = CalculateVolume(segment.buffer.AsSpan(0, segment.length));
            
            Log.Verbose("[VAD] Volume: {0}", volume.ToString("F4"));
            
            return volume >= volumeThreshold;
        }

        private static float CalculateVolume(Span<float> samples)
        {
            var sum = 0f;
            foreach (var sample in samples)
            {
                sum += sample * sample;
            }

            return Mathf.Sqrt(sum / samples.Length); // Root mean square
        }
    }
}