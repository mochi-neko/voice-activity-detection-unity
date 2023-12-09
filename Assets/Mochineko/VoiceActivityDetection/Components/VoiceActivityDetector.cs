#nullable enable
using System;
using System.IO;
using System.Threading;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Components
{
    public sealed class VoiceActivityDetector : MonoBehaviour, IWaveStreamReceiver
    {
        [SerializeField]
        private SourceType sourceType = SourceType.Microphone;

        [SerializeField]
        private BufferType bufferType = BufferType.None;

        [SerializeField]
        private LogicType logicType = LogicType.Cumulative;

        [SerializeField]
        private CumulativeLogicParameters cumulativeLogicParameters = new();

        [SerializeField]
        private bool echo = false;

        [SerializeField]
        private AudioSource? echoAudioSource = null;

        [SerializeField]
        private BoolEvent onActive = new();

        public BoolEvent OnActive => onActive;

        [SerializeField]
        private AudioClipEvent onActiveAudioClip = new();

        public AudioClipEvent OnActiveAudioClip => onActiveAudioClip;

        [SerializeField]
        private StreamEvent onActiveWaveStream = new();

        public StreamEvent OnActiveWaveStream => onActiveWaveStream;

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly CompositeDisposable compositeDisposable = new();

        private IVoiceActivityDetector? logic = null;

        private void Start()
        {
            if (echo && echoAudioSource == null)
            {
                Log.Error("[VAD.Component] Echo is enabled but AudioSource is not attached.");
                throw new NullReferenceException(nameof(echoAudioSource));
            }

            var source = CreateSource();
            compositeDisposable.Add(source);

            var buffer = CreateBuffer(source);
            if (echo && bufferType != BufferType.AudioClip)
            {
                var echoBuffer = CreateEchoBuffer(source);
                buffer = new CompositeVoiceBuffer(buffer, echoBuffer);
            }

            compositeDisposable.Add(buffer);

            logic = CreateLogic(source, buffer);
            compositeDisposable.Add(logic);

            logic
                .VoiceIsActive
                .Subscribe(isActive =>
                {
                    Log.Debug("[VAD.Component] Change voice activity: {0}", isActive);
                    onActive.Invoke(isActive);
                })
                .AddTo(compositeDisposable);
        }

        private void OnDestroy()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            compositeDisposable.Dispose();
        }

        private void Update()
        {
            logic?.Update();
        }

        private void OnEnable()
        {
            logic?.SetDetectorActive(true);
        }

        private void OnDisable()
        {
            logic?.SetDetectorActive(false);
        }

        private IVoiceSource CreateSource()
        {
            switch (sourceType)
            {
                case SourceType.Microphone:
                    var proxy = new UnityMicrophoneProxy();
                    compositeDisposable.Add(proxy);
                    return new UnityMicrophoneSource(proxy);

                case SourceType.AudioSource:
                    if (GetComponent<AudioSource>() == null)
                    {
                        Log.Error("[VAD.Component] Voice source is AudioSource but is not attached.");
                        throw new NullReferenceException(nameof(AudioSource));
                    }

                    return new UnityAudioSource();

                default:
                    throw new ArgumentOutOfRangeException(nameof(sourceType));
            }
        }

        private IVoiceBuffer CreateBuffer(IVoiceSource source)
        {
            switch (bufferType)
            {
                case BufferType.None:
                    return new NullVoiceBuffer();

                case BufferType.AudioClip:
                    var audioClipBuffer = new AudioClipBuffer(
                        maxSampleLength: (int)((cumulativeLogicParameters.MaxCumulatedTimeSeconds + 1f) *
                                               source.SamplingRate),
                        frequency: source.SamplingRate);
                    audioClipBuffer
                        .OnVoiceInactive
                        .Subscribe(clip =>
                        {
                            Log.Debug("[VAD.Component] Publish AudioClip of buffered voice.");
                            onActiveAudioClip.Invoke(clip);

                            PlayEchoAudioClip(clip);
                        })
                        .AddTo(compositeDisposable);
                    return audioClipBuffer;

                case BufferType.WaveFileStream:
                    return new WaveVoiceBuffer(this, channels: source.Channels);

                default:
                    throw new ArgumentOutOfRangeException(nameof(bufferType));
            }
        }

        private IVoiceActivityDetector CreateLogic(IVoiceSource source, IVoiceBuffer buffer)
        {
            switch (logicType)
            {
                case LogicType.Cumulative:
                    return new CumulativeVoiceActivityDetector(
                        source,
                        buffer,
                        cumulativeLogicParameters.ActiveVolumeThreshold,
                        cumulativeLogicParameters.ActiveChargeTimeRate,
                        cumulativeLogicParameters.MaxChargeTimeSeconds,
                        cumulativeLogicParameters.MinCumulatedTimeSeconds,
                        cumulativeLogicParameters.MaxCumulatedTimeSeconds
                    );

                default:
                    throw new ArgumentOutOfRangeException(nameof(logicType));
            }
        }

        private IVoiceBuffer CreateEchoBuffer(IVoiceSource source)
        {
            var audioClipBuffer = new AudioClipBuffer(
                maxSampleLength: (int)((cumulativeLogicParameters.MaxCumulatedTimeSeconds + 1f) * source.SamplingRate),
                frequency: source.SamplingRate);

            audioClipBuffer
                .OnVoiceInactive
                .Subscribe(PlayEchoAudioClip)
                .AddTo(compositeDisposable);

            return audioClipBuffer;
        }

        private void PlayEchoAudioClip(AudioClip clip)
        {
            if (!echo)
            {
                return;
            }

            if (echoAudioSource == null)
            {
                return;
            }

            Log.Debug("[VAD.Component] Play echo AudioClip of buffered voice.");
            echoAudioSource.Stop();
            echoAudioSource.clip = clip;
            echoAudioSource.Play();
        }

        void IWaveStreamReceiver.OnReceive(Stream stream)
        {
            Log.Debug("[VAD.Component] Publish WaveStream of buffered voice.");
            onActiveWaveStream.Invoke(stream);
        }
    }
}
