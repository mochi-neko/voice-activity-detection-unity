# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2023-07-11

## Added
- Add echo sample to test VAD with hearing microphone audio.
- Add composite buffer to combine multiple buffers.
- Add validations of `UnityEngine.Microphone`.

## Changed
- Improve VAD logic and parameters.
- Improve recording of `UnityEngine.Microphone` when just before activated.

## Fixed
- Fix data duplication of `UnityEngine.Microphone` when recording position is not changed.

## [0.1.0] - 2023-07-05

### Added
- Voice source from `UnityEngine.Microphone`.
- Wave file Voice buffer by NAudio.
- Queueing-based VAD logic.
- A simple sample implementation of VAD as component.
- A sample implementation of VAD with OpenAI/Whisper API.