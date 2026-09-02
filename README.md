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

## Building

Windows binaries are produced by GitHub Actions (`.github/workflows/build.yml`)
for Release and Debug, x64 only. The projects can still target ARM64 locally
(`make build PLATFORM=ARM64`); CI just does not build it.

Locally, everything builds in a disposable container — only Docker is needed:

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
