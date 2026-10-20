# FlowWhisper

<img src="src/FlowWhisper.App/Assets/AppIcon.png" width="64" align="right" alt="" />

System-wide, fully local dictation for Windows. Hold a key, speak, release, and the
text appears in whatever app has focus: Notepad, a browser, Word, Claude Desktop and so on.
Speech recognition runs on your own machine with OpenAI's Whisper through Intel OpenVINO.
No audio or text leaves the computer.

Inspired by Wispr Flow, but its own app with its own UI. It was built first and foremost
for Swedish dictation, and it works in Whisper's other languages too.

## Features

- **Push-to-talk** with a configurable key (default: Right Shift).
- **Local speech recognition**: Whisper large-v3-turbo (best quality) or small (fastest),
  running on the GPU, NPU or CPU via OpenVINO.
- **Floating glass bar** with a live waveform, so you can see the microphone is picking you
  up, and optionally the text as you speak.
- **Pastes into any app**: your clipboard is restored afterwards and the dictation is kept
  out of clipboard history.
- **Choose your language**: the app UI follows Windows (Swedish or English), and you pick
  the language you speak separately (Swedish, English, Norwegian, Danish, Finnish, German,
  French, Spanish, or automatic detection).
- **Optional AI cleanup** (experimental): Phi-3.5-mini removes filler words and fixes
  punctuation. A guard rejects any result that adds or drops words.
- **History** of recent dictations, stored locally in SQLite.
- Filters for Whisper's classic hallucinations ("Thanks for watching", subtitle credits) and
  for silence.

## Requirements

- Windows 10 1809 or later / Windows 11, x64.
- An Intel GPU or NPU (Core Ultra, Iris Xe, Arc) for fast recognition. Other machines
  can use the CPU setting, which is slower.
- About 1 GB of disk space for the speech model and the OpenVINO runtime, downloaded on
  first start. AI cleanup needs another 2 GB.

## Install

1. Download `FlowWhisperApp-win-Setup.exe` from
   [Releases](../../releases) and run it. It installs per user, without admin rights.
2. The installer isn't code-signed, so Windows SmartScreen may warn the first time.
   Click **More info → Run anyway**.
3. On first start, FlowWhisper asks to download the speech model. Each file is
   checked against a pinned SHA-256 hash.
4. Hold **Right Shift**, speak, and release.

The app lives in the system tray, and right-clicking the icon opens Settings. To uninstall, go
to Windows **Settings → Apps**. Models and history in `%LOCALAPPDATA%\FlowWhisper` are
kept, so you can delete that folder too if you want everything gone.

## Build from source

You need the .NET 10 SDK (see `global.json`).

```powershell
dotnet build FlowWhisper.sln
dotnet test tests\FlowWhisper.Core.Tests
.\run-app.ps1                 # build and start the app (Debug)
.\publish.ps1 -Version 0.1.0  # self-contained build, zip and Setup.exe
```

`publish.ps1` packages the installer with [Velopack](https://velopack.io). Install its CLI
once:

```powershell
dotnet tool install -g vpk --version 1.2.161
```

Set `FLOWWHISPER_DATA_DIR` to run the app against a separate data folder (useful for
testing the first-run experience).

## Project layout

| Project | Contents |
|---|---|
| `src/FlowWhisper.App` | WinUI 3 app: tray icon, settings window, floating bar, wiring |
| `src/FlowWhisper.Core` | Hotkey hook, audio capture, OpenVINO Whisper/LLM bindings, text injection, downloads, localization |
| `src/FlowWhisper.Data` | Settings (JSON) and history (SQLite) |
| `tests/FlowWhisper.Core.Tests` | xUnit tests |

## Adding a UI language

All UI texts are in `src/FlowWhisper.Core/Localization/UiStrings.cs`, with one instance per
language. Every text is a `required` member, so a translation that misses one won't compile.
To add a language, add an instance and map its code in `LanguageResolver.ResolveUi`.

## Third-party components

- [OpenVINO GenAI](https://github.com/openvinotoolkit/openvino.genai) (Apache-2.0), downloaded at runtime
- [Whisper](https://github.com/openai/whisper) models (MIT), OpenVINO exports from the OpenVINO organization on Hugging Face
- [Phi-3.5-mini-instruct](https://huggingface.co/microsoft/Phi-3.5-mini-instruct) (MIT), optional
- [NAudio](https://github.com/naudio/NAudio), [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon),
  [sqlite-net](https://github.com/praeclarum/sqlite-net), [Velopack](https://github.com/velopack/velopack)
