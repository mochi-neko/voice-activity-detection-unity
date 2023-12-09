#nullable enable
using System;
using System.IO;
using UniRx;
using Unity.Logging;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection.Components
{
    /// <summary>
    /// A component implementation of IVoiceActivityDetector.
    /// </summary>
    public sealed class VoiceActivityDetector
        : MonoBehaviour, IWaveStreamReceiver
    {
        [SerializeField, Tooltip("Source type of voice activity detection.")]
        private SourceType sourceType = SourceType.Microphone;

        [SerializeField, Tooltip("Buffer type of voice activity detection.")]
        private BufferType bufferType = BufferType.None;

        [SerializeField, Tooltip("Logic type of voice activity detection.")]
        private LogicType logicType = LogicType.Cumulative;

        [SerializeField, Tooltip("Parameter set for cumulative logic.")]
        private CumulativeLogicParameters cumulativeLogicParameters = new();

        [SerializeField, Tooltip("Whether echo voice buffer or not.")]
        private bool echo = false;

        [SerializeField, Tooltip("[Optional] AudioSource to echo voice buffer.")]
        private AudioSource? echoAudioSource = null;

        private readonly ReactiveProperty<bool> isActive = new(false);

        /// <summary>
        /// Reactive property of voice activity.
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsActive => isActive;

        [SerializeField, Tooltip("Called when voice activity has been changed.")]
        private BoolEvent onActive = new();

        /// <summary>
        /// Called when voice activity has been changed.
        /// </summary>
        public BoolEvent OnActive => onActive;

        private readonly Subject<AudioClip> onActiveAudioClipSubject = new();

        /// <summary>
        /// Observable of AudioClip of active voice.
        /// </summary>
        /// <returns></returns>
        public IObservable<AudioClip> OnActiveAudioClipAsObservable() => onActiveAudioClipSubject;

        [SerializeField, Tooltip("Called when new AudioClip of active voice has been created.")]
        private AudioClipEvent onActiveAudioClip = new();

        /// <summary>
        /// Called when new AudioClip of active voice has been created.
        /// </summary>
        public AudioClipEvent OnActiveAudioClip => onActiveAudioClip;

        private readonly Subject<Stream> onActiveWaveStreamSubject = new();

        /// <summary>
        /// Observable of Stream of active voice.
        /// </summary>
        /// <returns></returns>
        public IObservable<Stream> OnActiveWaveStreamAsObservable() => onActiveWaveStreamSubject;

        [SerializeField, Tooltip("Called when new Stream of active voice has been created.")]
        private StreamEvent onActiveWaveStream = new();

        /// <summary>
        /// Called when new Stream of active voice has been created.
        /// </summary>
        public StreamEvent OnActiveWaveStream => onActiveWaveStream;

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
                    this.isActive.Value = isActive;
                    onActive.Invoke(isActive);
                })
                .AddTo(compositeDisposable);
        }

        private void OnDestroy()
        {
            isActive.Dispose();
            onActiveAudioClipSubject.Dispose();
            onActiveWaveStreamSubject.Dispose();
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
                        .Subscribe(onActiveAudioClipSubject)
                        .AddTo(compositeDisposable);

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
            onActiveWaveStreamSubject.OnNext(stream);
            onActiveWaveStream.Invoke(stream);
        }
    }
}
