# voice-activity-detection-unity
A voice activity detection library for Unity.

## Features

- Sources
  - [x] `UnityEngine.Microphone`
  - [ ] `AudioSource`
  - [ ] Native microphone
- Buffers
  - [x] Null
  - [x] Wave file (by [simple-audio-codec-unity](https://github.com/mochi-neko/simple-audio-codec-unity) / [NAudio](https://github.com/naudio/NAudio))
  - [ ] AudioClip
- Voice activity detection logics
  - [x] Simple (voice volume and false interval)
  - [ ] WebRTC VAD

## How to import by UnityPackageManager

Add following dependencies to your `/Packages/manifest.json`.

```json
{
    "dependencies": {
        "com.mochineko.voice-activity-detection": "https://github.com/mochi-neko/voice-activity-detection-unity?path=/Assets/Mochineko/VoiceActivityDetection#0.1.0",
        ...
    }
}
```

## Change log

See [CHANGELOG](./CHANGELOG.md).

## 3rd party notices

See [NOTICE](./NOTICE.md).

## License

Licensed under the [MIT](./LICENSE) license.
