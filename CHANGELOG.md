# Changelog

All notable changes to FlowWhisper are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project uses [Semantic Versioning](https://semver.org/).

## [Unreleased] – experimental/backtracking

### Added

- "Understand corrections" (experimental, off by default): say "kl 12, nej förresten kl 13"
  and only "kl 13" is typed. Four modes:
  - **Off**.
  - **Rules:** instant; handles times (also spelled out), numbers, weekdays and single
    words or names, in Swedish and English.
  - **Rules + AI when needed:** the local Phi-3.5 model is only asked when a correction
    phrase was said and no rule fit.
  - **AI on everything:** the model reads every dictation; slower.
- The model's answer is only used if it has the exact shape of a correction: one run of
  words removed, ending with the correction phrase, and nothing else changed. Otherwise the
  dictation is used as said.
- History shows whether a correction was made by rules or AI, or whether the AI answer
  was rejected.

## [0.1.9] - 2026-10-21

### Changed

- The dictation bar is flat frosted glass with no edge at all: no sheen, highlight streak,
  rim, hairline or Windows window outline.
- In remote desktops (Omnissa Horizon, Citrix, Microsoft Remote Desktop) the dictation
  is put on the clipboard and the bar asks you to press Ctrl+V. Horizon ignores keystrokes
  synthesized by other programs, so an automatic paste never arrived. The text stays on
  the clipboard until you paste it.

### Fixed

- The dictation key wasn't noticed while a Horizon window had focus: Horizon's own
  keyboard hook swallowed it. FlowWhisper now puts its hook first again after every focus
  change.
- Ctrl+V is sent with hardware scan codes, which remote-desktop clients need.

## [0.1.5] - 2026-10-21

### Changed

- The splash window has a minimal edge: one faint hairline instead of the colored rim
  and Windows' window outline.

## [0.1.4] - 2026-10-21

### Added

- A splash window shows while FlowWhisper starts, then says it's ready and which key
  to hold.
- A "Bar transparency" setting (0–100 %) for the dictation bar and the splash. Moving the
  slider shows the bar right away.
- Dictating into remote desktops (Omnissa Horizon, Citrix, Microsoft Remote Desktop):
  FlowWhisper detects the client, gives its clipboard redirection time to copy the text
  over before pasting, and restores the clipboard later.
- A note in Settings, with a link to Windows' keyboard settings, when Shift is the
  dictation key.

### Changed

- New installs use Right Ctrl as the dictation key. Holding Right Shift for 8 seconds
  makes Windows offer Filter Keys, and pressing Shift five times offers Sticky Keys.
  Existing settings are kept.

### Fixed

- Recording no longer gets stuck when a remote-desktop client swallows the key release.
  The keyboard hook moves ahead of the client's when dictation starts, and recording stops
  if the key stops auto-repeating, or after 3 minutes at the latest.

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

[0.1.9]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.5...v0.1.9
[0.1.5]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.4...v0.1.5
[0.1.4]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/ehsahog/FlowWhisper/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/ehsahog/FlowWhisper/releases/tag/v0.1.0
