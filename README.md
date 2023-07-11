# voice-activity-detection-unity
A voice activity detection (VAD) library for Unity.

## Features

Records voice data from any sources (`IVoiceSource`, e.g. recording by `UnityEngine.Microphone`)
 detects voice activity by any logic,
 and provides voice data to any buffers (`IVoiceBuffer`, e.g. buffering to WAV file) when voice is active.

You can customize voice sources, voice buffers,
 and voice activity detection logics adjusting your use cases.

- Sources
  - [x] `UnityEngine.Microphone` -> [UnityMicrophoneSource](./Assets/Mochineko/VoiceActivityDetection/UnityMicrophoneSource.cs)
  - [ ] `AudioSource`
  - [ ] Native microphone
- Buffers
  - [x] Null (Detection only) -> [NullVoiceBuffer](./Assets/Mochineko/VoiceActivityDetection/NullVoiceBuffer.cs)
  - [x] Wave file (by [NAudio](https://github.com/naudio/NAudio)) -> [WaveFileVoiceBuffer](./Assets/Mochineko/VoiceActivityDetection/WaveVoiceBuffer.cs)
  - [x] AudioClip -> [AudioClipBuffer](./Assets/Mochineko/VoiceActivityDetection/AudioClipBuffer.cs)
- Voice activity detection logics
  - [x] Queueing-based simple VAD logic -> [QueueingVoiceActivityDetector](./Assets/Mochineko/VoiceActivityDetection/QueueingVoiceActivityDetector.cs)

## How to import by UnityPackageManager

Add following dependencies to your `/Packages/manifest.json`.

```json
{
    "dependencies": {
        "com.mochineko.voice-activity-detection": "https://github.com/mochi-neko/voice-activity-detection-unity?path=/Assets/Mochineko/VoiceActivityDetection#0.2.0",
        ...
    }
}
```

## Samples

- [VAD as component](./Assets/Mochineko/VoiceActivityDetection.Samples/VADSample.cs)
- [VAD with echo](./Assets/Mochineko/VoiceActivityDetection.Samples/VADAudioClipEchoSample.cs)
- [VAD with OpenAI/Whisper API transcription](./Assets/Mochineko/VoiceActivityDetection.Samples/VADToWhisperSample.cs)

See also [Samples](./Assets/Mochineko/VoiceActivityDetection.Samples).

## Change log

See [CHANGELOG](./CHANGELOG.md).

## 3rd party notices

See [NOTICE](./NOTICE.md).

## License

Licensed under the [MIT](./LICENSE) license.
