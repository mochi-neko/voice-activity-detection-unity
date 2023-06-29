#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class SimpleVoiceActivityDetection : IVoiceActivityDetection
    {
        private readonly IVoiceInput input;
        private readonly IVoiceOutput output;
        private readonly IDisposable disposable;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        
        private readonly float volumeThreshold;

        private readonly ReactiveProperty<bool> isActive = new();
        public IReadOnlyReactiveProperty<bool> IsActive => isActive;

        public SimpleVoiceActivityDetection(
            IVoiceInput input,
            IVoiceOutput output,
            float volumeThreshold)
        {
            this.input = input;
            this.output = output;
            this.volumeThreshold = volumeThreshold;

            disposable = this.input
                .OnBufferRead
                .Subscribe(async value => await OnBufferReadAsync(value));
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
                isActive.Value = false;
                return;
            }
            
            await output.WriteAsync(value.buffer, value.length, cancellationTokenSource.Token);
            
            isActive.Value = true;
        }
        
        private bool IsActiveVoice(float[] buffer, int length)
            => CalculateVolume(buffer.AsSpan(0, length)) >= volumeThreshold;

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