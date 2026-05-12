![Terminal Frequency Splash](https://github.com/lethargiesleeps/TERMINAL_FREQUENCY/blob/main/img/splash.jpg)

# TERMINAL__FREQUENCY
*v0.9.0*

## ABOUT
A Windows console program for audio visualizations. Captures any audio playing from your computer automatically. Features a robust settings API to endlessly customize the look and feel of the available visuals.
![Terminal Frequency Demo Sampler](https://github.com/lethargiesleeps/TERMINAL_FREQUENCY/blob/main/img/demo.gif)

### BUILT WITH
- .NET 8.0
- C#
- NAudio
- WIN32/KERNEL32 API

## GETTING STARTED
Download the [Latest Release](https://github.com/lethargiesleeps/TERMINAL_FREQUENCY/releases/tag/alpha) `.zip` file. Extract the archive and run the application from within the extracted folder.
The `settings.json` file should be in whatever folder the application is launched from. If one does not exist, it will be auto-generated.\
**It is highly recommended to initially run the program with the default settings.**\
If you've accidentally modified some settings that are causing issues you can launch the program, go into the visualizer, press F3 to restore to defaults, then F1 to save. This will overwrite any changes in `settings.json` back to defaults.\
**If the program crashes at launch, or crashes when entering the visualizer**, copy the contents of `settings_backup.json` from the repository, and paste it into your local `settings.json` where you are running the program. Each release will have an up-to-date backup in the `.zip` folder as well.
## USAGE
More stuff coming soon

### AUDIO CAPTURE
By default, TERMINAL_FREQUENCY uses the first available audio device on a system via WASAPI Loopback. Specific devices can be set in `settings.json` by settings *SpecifyAudioDevice* to `true` or `0`.
If `SpecifyAudioDevice` is `true`, the program no longer uses WASAPI Loopback. Devices can be set via `AudioDeviceIndex`, an invalid index will default to either first or last available device.\
**NOTE: NOT ALL DEVICES LISTED MAY PRODUCE VISUALIZATIONS, SOME PLAYING AROUND WILL BE NEEDED.**\
You can see a list of all available devices on the host system by setting `UserSelectedDevice` to `true` or `0`. This will open a prompt when launching the program that shows the list of devices, and their corresponding index to select. From here you can enter the corresponding value, this will bypass whatever is set via `AudioDeviceIndex`.\

#### SPECIFIC DEVICE CAPTURE AND USING MICROPHONE AS CAPTURE
The program may crash if certain Windows privacy settings are enabled. For example, if selected a microphone input as primary audio capture, you need to ensure the App has access to record or it will crash. Microphone permissions can bet set via searching *Microphone Privacy Settings* in the Windows start menu and allowing apps to access it.\
**For privacy reasons, TERMINAL_FREQUENCY does not store recorded audio, you can confirm this by seeing Core.Audio.AudioCapture code as well as Core.Audio.FftAnalyzer. Audio is only recorded to process volumes, frequencies and stereo imaging to properly visualize.**\

#### DEVICE ROUTING
Some external audio interfaces may need to be routed to WASAPI Loopback default device. To acheive this, ensure `SpecifyAudioDevice` is set to `false` or `0`. Then open up an audio mixer program (default Windows one may not solve this), and route the audio from the external interface to the default device.
Voicemeeter [Banana](https://vb-audio.com/Voicemeeter/banana.htm) and [Potato](https://vb-audio.com/Voicemeeter/potato.htm) are free-to-use donationware that can temporarily fix this limitation, as I try to figure out a more permanent solution to this bottle-neck.

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
- Equalizer

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
- 2 additional dynamic visualizations (equalizer added as of v0.8)
- 2 static visualizations (visuals that don't react to audio)
- Audio interface selection (completed in v0.9)
- Background manipulation (change colour on beat, change colour every Nth minute)
- Trigger on audio frequency (completed in v0.9)
- Mic input capture (completed in v0.9, by allowing user to manually select the input device for capture)
- More comprehensive settings API documentation (completed in v0.8) see [SETTINGS.md](https://github.com/lethargiesleeps/TERMINAL_FREQUENCY/tree/main/SETTINGS.md)

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
