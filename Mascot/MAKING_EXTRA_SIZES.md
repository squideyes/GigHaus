# Making extra sizes of the transparent WebM

Nov 5, 2026 · @Louis Berman

To add a size, run one ffmpeg command with the new size in it, then check the result. It takes under 30 seconds per file.

## What you have now

Five silent VP9 WebM files with a transparent background sit next to `input2.mp4` in the ffmpeg `bin` folder. All are made directly from the 1440x1440 source and loop for 3.58 seconds at 24 fps, cut from the 5-second clip.

| File | Size (px) | File size | Encode time |
| --- | --- | --- | --- |
| `output-128.webm` | 128x128 | 0.18 MB | 4 s |
| `output-192.webm` | 192x192 | 0.27 MB | 4 s |
| `output-256.webm` | 256x256 | 0.37 MB | 5 s |
| `output-384.webm` | 384x384 | 0.57 MB | 6 s |
| `output-512.webm` | 512x512 | 0.71 MB | 7 s |

## Make a new size

Open PowerShell in the ffmpeg `bin` folder, set `$size`, and run the command. This example makes `output-640.webm`.

```powershell
$size = 640
.\ffmpeg.exe -n -i input2.mp4 -an -filter_complex "[0:v]trim=start_frame=9:end_frame=95,setpts=PTS-STARTPTS,format=yuva444p,chromakey=0x0CE905:0.16:0.05,split[c][m];[m]alphaextract,erosion,erosion,erosion[e];[c][e]alphamerge,format=gbrap,premultiply=inplace=1,scale=${size}:${size}:flags=lanczos,unpremultiply=inplace=1,format=yuva420p[v]" -map "[v]" -c:v libvpx-vp9 -pix_fmt yuva420p -crf 24 -b:v 0 -auto-alt-ref 0 -row-mt 1 "output-$size.webm"
```

For several sizes at once, wrap the same command in a loop:

```powershell
foreach ($size in 96, 640, 768) {
    .\ffmpeg.exe -n -i input2.mp4 -an -filter_complex "[0:v]trim=start_frame=9:end_frame=95,setpts=PTS-STARTPTS,format=yuva444p,chromakey=0x0CE905:0.16:0.05,split[c][m];[m]alphaextract,erosion,erosion,erosion[e];[c][e]alphamerge,format=gbrap,premultiply=inplace=1,scale=${size}:${size}:flags=lanczos,unpremultiply=inplace=1,format=yuva420p[v]" -map "[v]" -c:v libvpx-vp9 -pix_fmt yuva420p -crf 24 -b:v 0 -auto-alt-ref 0 -row-mt 1 "output-$size.webm"
}
```

Four rules keep the result clean:

- **Start from `input2.mp4`.** Resizing an existing WebM loses quality twice and can drop the alpha channel.
- **Stay at or below 1440.** That is the source size, so anything larger is upscaled and goes soft.
- **Use even numbers.** The video format stores colour at half resolution.
- **Keep `${size}` in braces.** PowerShell reads `$size:` as something else and the command breaks.

The `-n` flag stops ffmpeg overwriting a file that already exists. To redo a size, delete its file first or change `-n` to `-y`. The `-an` flag is what leaves the audio out. The trim filter sets the loop, so keep it the same for every size.

## Check the new file

Two quick checks confirm the size, the alpha channel and the missing audio. With `$size` still set, run the first one:

```powershell
.\ffprobe.exe -v error -show_entries stream=codec_type,width,height:stream_tags=alpha_mode -of compact "output-$size.webm"
```

You should get exactly one line, like this. A second line means an audio stream is present, and a missing `alpha_mode=1` means there is no transparency.

```
stream|codec_type=video|width=640|height=640|tag:alpha_mode=1
```

Then look at a frame on a magenta background, which makes holes and green edges easy to spot:

```powershell
.\ffmpeg.exe -y -c:v libvpx-vp9 -i "output-$size.webm" -f lavfi -i "color=magenta:s=${size}x${size}" -frames:v 1 -filter_complex "[1:v][0:v]overlay=format=auto" "check-$size.png"
```

Open `check-640.png`. The character should sit on solid magenta with no green and no pink showing through the gloves or shoes.

Keep `-c:v libvpx-vp9` before `-i` in that command. Without it, ffmpeg's built-in VP9 decoder ignores the alpha channel and the check tells you nothing. For the same reason, `ffprobe` reports `pix_fmt=yuv420p` for these files, which is normal.

## Choose a size and use it

Pick a file about twice the size it appears on the page if you want it sharp on high-density screens. For example, use `output-512.webm` for a video displayed at 256 pixels.

```html
<video src="output-512.webm" autoplay muted loop playsinline width="256" height="256"></video>
```

File size grows more slowly than pixel count. Going from 256 to 512 is 4 times the pixels but only 1.9 times the file.

To trade quality against file size, change `-crf 24`. A lower number means better quality and a bigger file. A higher number means the opposite.

## How the loop works

The videos play frames 9 to 94 of `input2.mp4`, because the full 5 seconds has a pause built in. The character is nearly still for the first 8 frames and slows almost to a stop over the last 6, which adds about 0.6 seconds of stillness to every loop.

In the command, `end_frame` is the frame after the last one played, so `95` ends on frame 94. To get the full clip back, remove `trim=start_frame=9:end_frame=95,setpts=PTS-STARTPTS,` from the command.

Seam is the difference between the last frame and the first. Normal steps in this clip have a median of about 3.

| Cut | Length | Seam | Result |
| --- | --- | --- | --- |
| Frames 9 to 94 | 3.58 s | 2.9 | Used. It matches the motion either side, 2.3 and 2.6, and keeps moving forward |
| Frames 11 to 72 | 2.58 s | 3.2 | Slightly closer match, but a full second shorter |
| Whole clip, 0 to 120 | 5.04 s | 6.1 | Rejected. About 0.6 s of stillness, then a visible jump |

A small seam is not enough on its own. It also has to match the motion around it and keep moving the same way. Crossfades are not used, because they double the image.

If `input2.mp4` changes, these frame numbers no longer apply and a new pair has to be found. In Chromium the jump back to the start took 67 to 83 ms against a normal 50 ms. That is less than one extra frame, it comes from the browser's `loop` handling, and re-encoding will not change it.

## If something looks wrong

Changing the size should not change the key settings, because the background is removed at full resolution before scaling. These fixes are for a re-edited or different source video.

| What you see | Likely cause | Fix |
| --- | --- | --- |
| Holes or pink in the character | First key number too high | Lower it. `0.28:0.05` left holes |
| Green patches left behind | First key number too low | Raise it. `0.10`, `0.16` and `0.22` all gave solid masks on this clip |
| Green rim around the outlines | Green mixed in during scaling | Keep the `premultiply`, `unpremultiply` and `erosion` filters in the command |
| Eyes turn dark navy | The `despill` filter was added | Remove it, because it shifts the blues |
| Nothing is removed | Background colour has changed | Sample it again, as below |
| Solid background in Safari | Safari's WebM alpha support | See the Safari note below |

The key numbers in `chromakey=0x0CE905:0.16:0.05` are similarity and blend. This bright green leaves a wide safe range. Test any change on a single frame before encoding the whole clip.

To sample the colour at pixel 0,0 of a new video, run:

```powershell
.\ffmpeg.exe -v error -y -i input2.mp4 -frames:v 1 -vf "format=rgb24,crop=1:1:0:0" -f rawvideo pixel.rgb
Format-Hex pixel.rgb
```

The three bytes shown are the colour. This clip's green varies slightly across the frame, so `0x0CE905` is a typical value from several samples, not a single pixel. Keep `format=rgb24` before `crop`, or ffmpeg rejects the 1x1 crop.

**Safari:** these files were tested in Chromium only, not Firefox or Safari. From memory, Safari does not reliably show alpha in WebM, especially on iOS. The usual fix is a second HEVC-with-alpha file, encoded on a Mac, listed first in a `<source>` element. Check current Safari behaviour before relying on this.
