![Terminal Frequency Splash](https://github.com/lethargiesleeps/TERMINAL_FREQUENCY/tree/main/img/splash.jpg)

# TERMINAL__FREQUENCY
*v0.8a*

## ABOUT
A Windows console program for audio visualizations. Captures any audio playing from your computer automatically. Features a robust settings API to endlessly customize the look and feel of the available visuals.
![Terminal Frequency Demo Sampler](https://github.com/lethargiesleeps/TERMINAL_FREQUENCY/tree/main/img/demo.gif)

### BUILT WITH
- .NET 8.0
- C#
- NAudio
- WIN32/KERNEL32 API

## GETTING STARTED
Coming soon

## USAGE
More stuff coming soon

### NOTE ON AUDIO CAPTURE
Currently, the program only registers the first audio device that is outputting audio (speakers, headset, bluetooth).
The first official release will let you select which audio interface to capture.
For the time being, an external tool can be used to route other audio so the default device is captured (if using an external audio interface or DJ controller).

Voicemeeter [Banana](https://vb-audio.com/Voicemeeter/banana.htm) and [Potato](https://vb-audio.com/Voicemeeter/potato.htm) are free-to-use donationware that can temporarily fix this limitation.
### CONTROLS
**GLOBAL:**
- TAB: Change visuals
- ESC: Exit program
- D: Toggle Debug Mode on/off
- SPACE: Pause/Resume
- L: Lock/unlock all controls (except ESC and D)
- F5: Toggle Exclusive Mode (full screen)
- F1: Save settings
- F2: Load last saved settings
- F3: Restore settings to default (does not save)

Each visualization mode has specific controls, toggle on the debug bar to view them.

### VISUAL MODES
As of *v0.8a* there are only 3 visual modes, but many more will be added. This version is basically just the audio processing/buffer rendering engine implementation.
Modes can be changed in the visualizer by pressing the TAB key.
As of this version, the Visual Modes are:

- Rings
- Waterfall
- Shape

### USER CONFIGURATION
In the built package, there is a `settings.json` with all the configurable settings for maximum customization. Once saved, you can press F2 in the visualizr to load the settings (you don't need to close the settings file).
Alternatively, each mode has some hotkeys to change a limited set of settings.

You can check out [SETTINGS.md](https://github.com/lethargiesleeps/TERMINAL_FREQUENCY/tree/main/SETTINGS.md) for a full reference of all available customization options (also coming soon).

### RENDERING MODES
**TERMINAL_FREQUENCY** comes bundled with 4 Rendering Modes. They can be toggled by pausing the visualizer and pressing the M key, or by editing RendererMode in *settings.json* and re-loading your settings. Each can be used to achieve different results, lower FPS ones can produce cool stuttering effects. In order of lowest to highest achievable FPS they are:
- PerPixel
- DirtyRect
- RowBatched
- DirectWrite

#### PerPixel
Renders the buffer one cell at a time.

#### DirtyRect
Only renders cells in the window that have changed since the last render cycle.

#### RowBatched
Renders an entire row at once. Extremely fast but has a duochrome limitation (only one background and foreground colour can be rendered). These colours can be edited in the settings file.

#### DirectWrite
Fastest of all the modes, writes and entire buffer to the window every fraction of a second. Has some character encoding limitations if using TrueType fonts, but a raster font can be used to achieve better results.
This is the default mode, and raster fonts are used by default in this mode. All this can be modified in the settings file.


## ROADMAP
### v1.0a
*v1.0a* is planned to include the following features:
- 3-5 additional dynamic visualizations
- 2-4 static visualizations (visuals that don't react to audio)
- Audio interface selection
- Background manipulation (change colour on beat, change colour every Nth minute)
- Trigger on audio frequency (right now it triggers based on volume parameters)
- Mic mode (capture audio from a microphone)
- More comprehensive settings API documentation

### v1.0a+
The following are features that are planned but may or may not be in v1.0a
- Scripting API
	- User can write a script using a super simple syntax, and run it while the program is open.
	- Time conditions *(ex: Over 10 seconds increase threshold by 0.02, After 3 minutes, change WaterfallColor to red)*
	- Audio conditions *(ex: if volume reaches 100, change visualization mode to Shape where ShapeType is Square)*
	- Regular conditions *(ex: once MaxRadius is 200, change BackgroundColor to Yellow)*
	- Simple iterations *(ex: Change ShapeColor 10 time between Red and Green)*
- Terminal based GUI to edit settings in real-time in a seperate window instead of by editing `settings.json`.
- Proper CLI implementation (launch program via console with arguments)
- LAN Mode, seperate devices can sync visuals of a local network (control visuals from laptop being used for a live performance to another laptop plugged into a projector).

## CONTRIBUTION
Coming soon, but feel free to fork project and submit PRs :)