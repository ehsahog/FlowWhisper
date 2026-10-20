# Changelog

All notable changes to FlowWhisper are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project uses [Semantic Versioning](https://semver.org/).

## [0.1.3] - 2026-10-21

### Added

- "Language you speak" now offers all 100 languages Whisper can transcribe, each shown
  by its own name and sorted alphabetically. Previously it offered eight.
- On a Windows display language that Whisper supports, dictation now follows it by
  default, e.g. Japanese, Polish or Filipino (Tagalog).

### Fixed

- If the selected model doesn't know the chosen language (the small model lacks
  Cantonese), FlowWhisper now detects the language automatically instead of failing.

## [0.1.2] - 2026-10-21

### Fixed

- FlowWhisper could run twice, for example after the installer started it and it was then
  opened from the Start menu. Both copies reacted to the dictation key at once, so the bar
  could hang on "Listening..." and one copy could use outdated settings, such as the wrong
  language. Now only one copy runs, and starting it again opens Settings in the running one.

## [0.1.1] - 2026-10-20

### Fixed

- The app crashed when Settings was opened from the tray after the settings window had
  been closed with its close button. Closing the window now only hides it, and the app
  keeps running in the tray.

## [0.1.0] - 2026-10-20

First public release.

### Added

- Push-to-talk dictation in any app, with a configurable key (default: Right Shift).
- Local speech recognition with Whisper large-v3-turbo or small via OpenVINO, on GPU,
  NPU or CPU. No audio or text leaves the computer.
- Floating glass bar with a live waveform and optional live text preview.
- Text inserted by pasting. The clipboard is restored afterwards and the dictation is kept
  out of clipboard history.
- App UI in Swedish and English, following the Windows display language by default.
- Selectable dictation language: Swedish, English, Norwegian, Danish, Finnish, German,
  French, Spanish, or automatic detection.
- Optional experimental AI cleanup with Phi-3.5-mini, guarded against added or dropped words.
- Local history of recent dictations.
- First-run download of models and runtime, checked against pinned SHA-256 hashes and
  able to resume.
- Detection of silent microphones and filtering of Whisper hallucinations.
- Per-user installer (Velopack) and a portable zip.

[0.1.3]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/ehsahog/FlowWhisper/releases/tag/v0.1.0
