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
    public sealed class SimpleVoiceActivityDetection : IVoiceActivityDetection
    {
        private readonly IVoiceInput input;
        private readonly IVoiceOutput output;
        private readonly IDisposable disposable;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Stopwatch stopwatch = new();
        
        private readonly float volumeThreshold;
        private readonly float intervalThresholdSeconds;

        private readonly ReactiveProperty<bool> isActive = new();
        public IReadOnlyReactiveProperty<bool> IsActive => isActive;

        public SimpleVoiceActivityDetection(
            IVoiceInput input,
            IVoiceOutput output,
            float volumeThreshold,
            float intervalThresholdSeconds)
        {
            this.input = input;
            this.output = output;
            this.volumeThreshold = volumeThreshold;
            this.intervalThresholdSeconds = intervalThresholdSeconds;

            disposable = this.input
                .OnBufferRead
                .Subscribe(async value => await OnBufferReadAsync(value));
            
            stopwatch.Start();
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
            disposable.Dispose();
            output.Dispose();
            input.Dispose();
        }

        public void Update()
        {
            input.Update();
        }

        private async UniTask OnBufferReadAsync((float[] buffer, int length) value)
        {
            if (!IsActiveVoice(value.buffer, value.length))
            {
                if (stopwatch.ElapsedMilliseconds > intervalThresholdSeconds * 1000)
                {
                    Log.Verbose("[VAD] Active: false");
                    isActive.Value = false;
                }
                return;
            }
            
            stopwatch.Restart();
            
            await output.WriteAsync(value.buffer, value.length, cancellationTokenSource.Token);
            
            Log.Debug("[VAD] Active: true");
            isActive.Value = true;
        }

        private bool IsActiveVoice(float[] buffer, int length)
        {
            var volume = CalculateVolume(buffer.AsSpan(0, length));
            
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