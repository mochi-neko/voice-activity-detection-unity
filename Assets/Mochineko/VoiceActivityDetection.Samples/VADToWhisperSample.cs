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
    /// <summary>
    /// A sample of voice activity detection as a component.
    /// Input UnityEngine.Microphone and output WAV file, then transcribe voice into text by OpenAI/Whisper API.
    /// </summary>
    internal sealed class VADToWhisperSample : MonoBehaviour, IWaveStreamReceiver
    {
        [SerializeField]
        private VADParameters? parameters = null;

        [SerializeField]
        private AudioSource? audioSource = null;

        private UnityMicrophoneProxy? proxy;
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
            if (parameters == null)
            {
                throw new NullReferenceException(nameof(parameters));
            }
            if (audioSource == null)
            {
                throw new NullReferenceException(nameof(audioSource));
            }

            proxy = new UnityMicrophoneProxy();

            IVoiceSource source = new UnityMicrophoneSource(proxy);

            var audioClipBuffer = new AudioClipBuffer(
                maxSampleLength: (int)(parameters.MaxActiveDurationSeconds * source.SamplingRate),
                frequency: source.SamplingRate);

            audioClipBuffer
                .OnVoiceInactive
                .Subscribe(clip =>
                {
                    Log.Info("[VAD.Sample] OnInactive and receive AudioClip and play.");
                    audioSource.clip = clip;
                    audioSource.Play();
                })
                .AddTo(this);

            var buffer = new CompositeVoiceBuffer(
                new WaveVoiceBuffer(this), // To wave file and Whisper transcription API.
                audioClipBuffer // To AudioClip and AudioSource (echo debug).
            );

            vad = new QueueingVoiceActivityDetector(
                source,
                buffer,
                parameters.MaxQueueingTimeSeconds,
                parameters.MinQueueingTimeSeconds,
                parameters.ActiveVolumeThreshold,
                parameters.ActivationRateThreshold,
                parameters.InactivationRateThreshold,
                parameters.ActivationIntervalSeconds,
                parameters.InactivationIntervalSeconds,
                parameters.MaxActiveDurationSeconds);
        }

        private void OnDestroy()
        {
            vad?.Dispose();
            proxy?.Dispose();
        }

        private void Update()
        {
            vad?.Update();

            if (streamQueue.TryDequeue(out var stream))
            {
                Log.Info("[VAD.Samples] Dequeue wave stream.");

                TranscribeAsync(stream, this.GetCancellationTokenOnDestroy())
                    .Forget();
            }
        }

        void IWaveStreamReceiver.OnReceive(Stream stream)
        {
            Log.Info("[VAD.Samples] Enqueue wave stream.");

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

            Log.Info("[VAD.Samples] Begin to transcribe for audio stream: {0} bytes.", stream.Length);

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
                        // FIXME: Log.Debug is not working at this.
                        //Log.Debug("[VAD.Samples] Succeeded to transcribe into: {0}.", text);
                        Debug.LogFormat("[VAD.Samples] Succeeded to transcribe into: {0}.", text);
                    }

                    break;
                }
                // Retryable failure
                case IUncertainRetryableResult<string> retryable:
                {
                    // FIXME: Log.Error is not working at this.
                    //Log.Error("[VAD.Samples] Retryable failed to transcribe because -> {0}.", retryable.Message);
                    Debug.LogErrorFormat("[VAD.Samples] Retryable failed to transcribe because -> {0}.", retryable.Message);
                    break;
                }
                // Failure
                case IUncertainFailureResult<string> failure:
                {
                    // FIXME: Log.Error is not working at this.
                    //Log.Error("[VAD.Samples] Failed to transcribe because -> {0}.", failure.Message);
                    Debug.LogErrorFormat("[VAD.Samples] Failed to transcribe because -> {0}.", failure.Message);
                    break;
                }
                default:
                    throw new UncertainResultPatternMatchException(nameof(result));
            }
        }
    }
}
