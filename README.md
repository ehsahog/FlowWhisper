# FlowWhisper

<img src="src/FlowWhisper.App/Assets/AppIcon.png" width="64" align="right" alt="" />

System-wide, fully local dictation for Windows. Hold a key, speak, release, and the
text appears in whatever app has focus: Notepad, a browser, Word, Claude Desktop and so on.
Speech recognition runs on your own machine with OpenAI's Whisper through Intel OpenVINO.
No audio or text leaves the computer.

Inspired by Wispr Flow, but its own app with its own UI. It was built first and foremost
for Swedish dictation, and it works in Whisper's other languages too.

## Features

- **Push-to-talk** with a configurable key (default: Right Ctrl).
- **Local speech recognition**: Whisper large-v3-turbo (best quality) or small (fastest),
  running on the GPU, NPU or CPU via OpenVINO.
- **Floating glass bar** with a live waveform, so you can see the microphone is picking you
  up, and optionally the text as you speak.
- **Pastes into any app**: your clipboard is restored afterwards and the dictation is kept
  out of clipboard history.
- **Choose your language**: the app UI follows Windows (Swedish or English), and you pick
  the language you speak separately: any of Whisper's 100 languages, or automatic detection.
  Recognition quality varies a lot between languages, and Swedish, English and the large
  European languages work best.
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
4. Hold **Right Ctrl**, speak, and release. A short splash shows when FlowWhisper has started.

The app lives in the system tray, and right-clicking the icon opens Settings. To uninstall, go
to Windows **Settings → Apps**. Models and history in `%LOCALAPPDATA%\FlowWhisper` are
kept, so you can delete that folder too if you want everything gone.

## Remote desktops (VDI)

FlowWhisper works with Omnissa (VMware) Horizon, Citrix and Microsoft Remote Desktop, with
one extra keystroke. Click into the remote session, hold the dictation key, speak, and
release. The text is transcribed on your own PC and put on the clipboard, and the bar says
**press Ctrl+V**. The client then copies the text to the remote machine.

- **Why not paste automatically?** Horizon ignores keystrokes synthesized by other programs
  (protection against keyloggers), so an automatic Ctrl+V never arrives. Your own Ctrl+V does.
- **Clipboard redirection must be allowed from client to remote.** In Horizon this is a
  policy set by your administrator ("Configure clipboard redirection"). Test it by copying
  something locally and pasting it in the remote session.
- In this mode the dictation stays on the clipboard, because it has to be there until you
  paste. Your previous clipboard content is not restored.
- Remote clients grab the keyboard with their own hook. FlowWhisper puts its hook first
  again after every focus change, so the dictation key is seen inside the remote window.
  If a key release still gets lost, recording stops by itself once the key stops repeating,
  or after 3 minutes at the latest.
## Shift as the dictation key

Windows reacts to Shift by itself: holding Right Shift for 8 seconds offers **Filter Keys**,
and pressing Shift five times offers **Sticky Keys**. That's why the default key is Right Ctrl.
If you prefer Shift, turn those shortcuts off under **Settings → Accessibility → Keyboard**.
The FlowWhisper settings page has a button that opens it.
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
