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
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Samples
{
    /// <summary>
    /// A sample of voice activity detection as a component.
    /// Input UnityEngine.Microphone and output WAV file, then transcribe voice into text by OpenAI/Whisper API.
    /// </summary>
    internal sealed class VADToWhisperSample : MonoBehaviour, IWaveStreamReceiver
    {
        [SerializeField]
        private float activeVolumeThreshold = 0.01f;
        
        [SerializeField]
        private float maxQueueingTimeSeconds = 1f;
        
        [SerializeField]
        private float minQueueingTimeSeconds = 0.5f;
        
        [SerializeField]
        private float activationRateThreshold = 0.6f;
        
        [SerializeField]
        private float inactivationRateThreshold = 0.4f;
        
        [SerializeField]
        private float activationIntervalSeconds = 0.5f;
        
        [SerializeField]
        private float inactivationIntervalSeconds = 0.5f;
        
        [SerializeField]
        private float maxActiveDurationSeconds = 10f;

        private IVoiceActivityDetector? vad;

        private readonly Queue<Stream> streamQueue = new();
        private readonly IPolicy<string> policy = WhisperPolicyFactory.Build();

        private readonly TranscriptionRequestParameters requestParameters = new(
            file: "UnityMicVAD.wav",
            model: Model.Whisper1,
            language: "ja");

        private static readonly HttpClient httpClient = new();

        private void Start()
        {
            var buffer = new WaveVoiceBuffer(this);

            vad = new QueueingVoiceActivityDetector(
                source: new UnityMicrophoneSource(),
                buffer: buffer,
                maxQueueingTimeSeconds,
                minQueueingTimeSeconds,
                activeVolumeThreshold,
                activationRateThreshold,
                inactivationRateThreshold,
                activationIntervalSeconds,
                inactivationIntervalSeconds,
                maxActiveDurationSeconds);
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

        void IWaveStreamReceiver.OnReceive(Stream stream)
        {
            Log.Debug("[VAD.Samples] Enqueue wave stream.");
            
            streamQueue.Enqueue(stream);
        }

        private async UniTask TranscribeAsync(Stream stream, CancellationToken cancellationToken)
        {
            // API key must be set in environment variable.
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new NullReferenceException(nameof(apiKey));
            }

            Log.Debug("[VAD.Samples] Begin to transcribe for audio stream: {0} bytes.", stream.Length);
            
            // Dispose stream when out of scope.
            await using var _ = stream;
            
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
                                debug: false),
                    cancellationToken);

            switch (result)
            {
                // Success
                case IUncertainSuccessResult<string> success:
                {
                    var text = TranscriptionResponseBody.FromJson(success.Result)?.Text;
                    if (text != null)
                    {
                        // Log.Debug("[VAD.Samples] Succeeded to transcribe into: {0}.", text);
                        Debug.LogFormat("[VAD.Samples] Succeeded to transcribe into: {0}.", text);
                    }

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