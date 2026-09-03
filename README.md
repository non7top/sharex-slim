# ShareX-slim

A fork of [ShareX](https://github.com/ShareX/ShareX) stripped down to one job:
**take a screenshot, mark it up, save it.**

Everything ShareX does beyond that is gone — no uploaders, no URL shorteners, no
screen or GIF recording, no video converter, no OCR, no QR codes, no history
database, no browser extension host, no standalone image editor.

## What it still does

- **Capture** — fullscreen, monitor, active/selected window, region (normal,
  light and transparent styles), last region, scrolling capture, auto capture.
- **Annotate** — the editor from `ShareX.ScreenCaptureLib`: arrows, shapes, text,
  highlight, blur, pixelate, step numbers, crop and so on. Marking up a shot
  before it is written is the whole point of the fork, and there are two ways in:
  - **Region capture** opens straight into annotation mode, so you draw on the
    region-capture surface itself. (Turn it off with the advanced setting
    *Disable annotation support in region capture*.)
  - The **"Annotate image" after-capture task** opens the editor on the captured
    image, which is how you annotate fullscreen, monitor and window captures.
    Enable it under *After capture tasks*.

  Either way the editor runs before the file is saved, and Save/Save as/Copy from
  inside the editor feed the same after-capture pipeline.
- **Save** — to the screenshots folder using ShareX's name patterns, save-as
  dialog, thumbnails, copy image/file/path to clipboard, print, open in explorer,
  and run external "Actions" on the saved file.
- **Global hotkeys, tray icon, workflows and the quick task menu**, plus the
  actions toolbar and the after-capture window.
- **Image effects** presets, still applied as an after-capture task.

### Sized arrows

The arrow tool has a **Show length** toggle in its shape-options dropdown. With
it on, each arrow is labelled with its pixel length near the midpoint, rotated to
follow the arrow and kept upright. The number is the straight-line distance
between the arrow's endpoints.

## Downloads

Grab a build from [Releases](https://github.com/non7top/sharex-slim/releases):

- **`latest`** — a rolling prerelease, rebuilt on every push to `slim`.
- **`vX.Y.Z`** — a fixed version. Pushing a `v*.*.*` tag publishes one, and the
  tag also sets the version stamped into the binaries and the file names.

Release assets are permanent and need no GitHub login, unlike the artifacts
attached to individual workflow runs (those expire after 90 days).

## Building

GitHub Actions (`.github/workflows/build.yml`) builds x64 only and, for the
Release configuration, publishes two files plus their `.sha256` companions:

- `ShareX-slim-<version>-setup-x64.exe` — Inno Setup installer. Installs per-user
  or machine-wide, optional desktop/send-to/startup shortcuts, an "Annotate with
  ShareX-slim" Explorer context-menu entry, and an option to free the Print
  Screen key from the Snipping Tool. Its `AppId` differs from upstream ShareX's,
  so it can never upgrade or uninstall a real ShareX installation.
- `ShareX-slim-<version>-portable-x64.zip` — unzip and run; it ships a `Portable`
  marker file so all settings stay inside the folder.

The Debug configuration is still built as a compile check and uploads the raw
output folder. ARM64 remains a valid local target (`make build PLATFORM=ARM64`);
CI does not build it.

Locally, everything builds in a disposable container — only Docker is needed
(the container compiles the code; producing the installer needs Windows, since
Inno Setup runs there):

```sh
make build      # build ShareX.Slim.sln (Release|x64) in the container
make restore    # restore NuGet packages (cached in a named volume)
make shell      # interactive shell in the build container
make destroy    # remove the container, image and cache volumes
```

Override the defaults per invocation, e.g. `make build CONFIG=Debug PLATFORM=ARM64`
or `make build SLN=ShareX/ShareX.csproj`.

The container runs as the host user so build output stays host-owned, and it
compiles the Windows-targeted (WinForms) projects on Linux via
`EnableWindowsTargeting`. Running the result still requires Windows.

On Windows with Visual Studio, open `ShareX.Slim.sln` and build `Release|x64`.

## Projects

| Project | Role |
| --- | --- |
| `ShareX` | WinForms host, Avalonia UI, hotkeys, tray, task pipeline |
| `ShareX.ScreenCaptureLib` | Screen capture and the annotation editor |
| `ShareX.HelpersLib` | Shared helpers (imaging, clipboard, name parser, ...) |
| `ShareX.ImageEffectsLib` | Image effects, used by the editor and after-capture |
| `ShareX.Avalonia` | Shared Avalonia theming, controls and bootstrap |
| `ShareX.Setup` | Packaging tool: builds the installer and the portable zip |

## Relationship to upstream

Forked from ShareX `develop` at v21.0.0 (`152da0980`). The `sharex` git remote
points at the source clone, so upstream changes can still be fetched and
cherry-picked; the slim tree keeps upstream's file layout so those merges stay
possible.

ShareX-slim stores its settings in `Documents\ShareX-slim` and uses its own
single-instance mutex, so it can be installed and run alongside real ShareX.

Links in the About window still point at the upstream ShareX project, which is
also where credit for all of this code belongs.

## License

GPL-3.0, inherited from ShareX. See [LICENSE.txt](LICENSE.txt).
