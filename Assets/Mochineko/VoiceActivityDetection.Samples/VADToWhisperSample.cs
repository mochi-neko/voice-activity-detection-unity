#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using Assets.Mochineko.WhisperAPI;
using Cysharp.Threading.Tasks;
using Mochineko.Relent.Resilience;
using Mochineko.Relent.UncertainResult;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    internal sealed class VADToWhisperSample : MonoBehaviour
    {
        [SerializeField]
        private float activeVolumeThreshold = 0.01f;
        
        [SerializeField]
        private float maxQueueingTimeSeconds = 1f;
        
        [SerializeField]
        private float activationRateThreshold = 0.6f;
        
        [SerializeField]
        private float deactivationRateThreshold = 0.4f;
        
        [SerializeField]
        private float intervalSeconds = 0.5f;

        private IVoiceActivityDetector? vad;

        private readonly Queue<Stream> streamQueue = new();
        private readonly IPolicy<string> policy = WhisperPolicyFactory.Build();

        private readonly TranscriptionRequestParameters requestParameters = new(
            "UnityMicVAD.wav",
            Model.Whisper1);

        private static readonly HttpClient httpClient = new();

        private void Start()
        {
            var buffer = new WaveVoiceBuffer();
            buffer
                .OnBufferCompleted
                .Subscribe(stream =>
                {
                    Log.Debug("[VAD.Samples] Enqueue wave stream.");
                    streamQueue.Enqueue(stream);
                })
                .AddTo(this);

            vad = new QueueingVoiceActivityDetector(
                source: new UnityMicrophoneSource(),
                buffer: buffer,
                maxQueueingTimeSeconds,
                activeVolumeThreshold,
                activationRateThreshold,
                deactivationRateThreshold,
                intervalSeconds
                );
        }

        private void OnDestroy()
        {
            vad?.Dispose();
        }

        private void Update()
        {
            vad?.Update();

            if (streamQueue.TryDequeue(out var stream))
            {
                Log.Debug("[VAD.Samples] Dequeue wave stream.");
                TranscribeAsync(stream, this.GetCancellationTokenOnDestroy())
                    .Forget();
            }
        }

        private async UniTask TranscribeAsync(Stream stream, CancellationToken cancellationToken)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new NullReferenceException(nameof(apiKey));
            }

            Log.Debug("[VAD.Samples] Begin to transcribe.");

            // Transcribe speech into text by Whisper transcription API.
            var result = await policy
                .ExecuteAsync(async innerCancellationToken
                        => await TranscriptionAPI
                            .TranscribeAsync(
                                apiKey,
                                httpClient,
                                stream,
                                requestParameters,
                                innerCancellationToken,
                                debug: true),
                    cancellationToken);

            switch (result)
            {
                // Success
                case IUncertainSuccessResult<string> success:
                {
                    var text = TranscriptionResponseBody.FromJson(success.Result)?.Text;
                    Log.Debug("[VAD.Samples] Succeeded to transcribe into: {0}.", text);
                    break;
                }
                // Retryable failure
                case IUncertainRetryableResult<string> retryable:
                {
                    Log.Error("[VAD.Samples] Retryable failed to transcribe because -> {0}.", retryable.Message);
                    break;
                }
                // Failure
                case IUncertainFailureResult<string> failure:
                {
                    Log.Error("[VAD.Samples] Failed to transcribe because -> {0}.", failure.Message);
                    break;
                }
                default:
                    throw new UncertainResultPatternMatchException(nameof(result));
            }
        }
    }
}