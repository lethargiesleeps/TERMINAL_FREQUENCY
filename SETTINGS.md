# SETTINGS
Documentation for all avaialable settings.
Can be modified in the `settings.json` file.
**NOTE:** Values that would cause the program to crash may be clamped to the nearest acceptable value or set to a default. Please let me know of any values that cause a crash to be hotfixed :)
**NOTE:** Any enum accepted value is in order, instead of text a zero-indexed integer can be used to select the desired setting starting:
```
DefaultMode: "Rings"

is the same as 

DefaultMode: 0

An incorrect number value will result in the default being used.
```


## Global
Global settings can be used to adjust program launch behaviour.

### BypassStartupScreen
#### true/false | bool
Skips the startup screen and launches directly into the selected visualization.

### ForceDefaultSettings
#### true/false | bool
If true, ignores the settings.json file and always uses program defaults.

### EnableDebugMode
#### true/false | bool
Displays extra debug information including visualization controls, volume levels, and FPS counter.

### EnableSafeMode
#### true/false | bool
If true, clamps setting values to their safe ranges when loading from JSON.

### EnableErrorMode [NOT IMPLEMENTED]
#### true/false | bool
If true, prints all errors for values outside safe ranges to the console and pauses for input.

### EnableControlLock
#### true/false | bool
Locks all keyboard controls except Debug Mode, Exit, and Unlock. Prevents accidental input during performance.

### ShowGlobalControls
#### true/false | bool
Displays global keyboard control hints in the debug bar.

### DefaultMode
#### VisualizationMode | enum
The visualization displayed at program launch.
**Accepted Values:**
```
Rings
Waterfall
Shape
Equalizer
```

### ConsoleInstances
#### whole number | int
Number of independent console windows to launch. Each run a seperate instance of the program.
**NOTE:** Currently they all work off the same `settings.json` file, so caution when saving/loading
Safe Mode Range: 1 - 10

### SaveOnExit
#### true/false | bool
Automatically saves current settings to settings.json when the program exits.

### EnableRasterOnDirectWrite
#### true/false | bool
Switches to a raster font when using DirectWrite render mode. Fixes block character rendering on some systems. Reverts to default TrueType font when not using DirectWrite.

### EnableExclusiveMode
#### true/false | bool
Launches in exclusive fullscreen mode, hiding the taskbar and title bar.

## Font
Font settings dictate which type is displayed when rendering characters. Only 1 font can be rendered at a time. Fonts may vary from system to system, the default font will always be Consolas (default font on most systems).
Some fonts can introduce character encoding issues. Enabling raster font usage can alleviate some of these encoding issues but not all.
Raster fonts are the default when using the DirectWrite renderer, otherwise the Consolas TrueType font is used.

### EnableRasterFont
#### true/false | bool
If true, uses a raster font instead of TrueType. Raster fonts can fix character encoding issues.

### RasterFontType
#### RasterFontType | enum
The raster font size to use when EnableRasterFont is true.
**Accepted Values:**
```
FourBySix
SixByEight
EightByEight
SixteenByEight
FiveByTwelve
SevenByTwelve
EightByTwelve
SixteenByTwelve
TwelveBySixteen
TenByEighteen
TenByTwenty
```

### EnableCustomFont
#### true/false | bool
If true, uses the custom TrueType font specified below instead of the default console font.

### CustomFontFace
#### FontFace | enum
The TrueType font face to use when EnableCustomFont is true. Falls back to Consolas if unavailable on system.
**Accepted Values:**
```
CascadiaCode
CascadiaMono
Consolas
CourierNew
LucidaConsole
LucidaSansTypeWriter
MSGothic
NSimSun
Terminal
```

### CustomFontFaceOverride
#### text | string
Overrides the CustomFontFace with any installed font name. Leave empty to use CustomFontFace. If the font is not found, falls back to Consolas.

### CustomFontSize
#### whole number | int
Font size in pixels when EnableCustomFont is true. Console character dimensions will adjust automatically.
Safe Mode Range: 8 - 48

### CustomFontBold
#### true/false | bool
If true, renders the custom font in bold weight (700). If false, uses normal weight (400).

## Rendering
Rendering settings can be used to set the active Rendering Mode (see RendererMode), dedicate more CPU usage to the program if needed or introduce deliberate slowdowns to achieve certain effects (see EnableYield/EnableSpinWait).
FPS can be seen with debug features enabled. Some rendering modes' FPS craters depending on screen size, but offer certain effects that may be desired.

### TargetFps
#### whole number | int
Target frames per second. Only applies when EnableYield or EnableSpinWait is enabled. When both are disabled, the program runs at maximum possible FPS.
Safe Mode Range: 10 - 500

### EnableYield
#### true/false | bool
If true, pauses the render thread for YieldTimeout milliseconds each frame. Prioritized over SpinWait. Note: Thread.Sleep is imprecise and will break accurate FPS calculations.

### YieldTimeout
#### whole number | int
Milliseconds to yield each frame when EnableYield is true. Higher values result in lower FPS. Approximate FPS: 1=~1000fps, 8=~120fps, 16=~60fps, 33=~30fps, 50=~20fps, 100=~10fps.
Safe Mode Range: 1 - 100

### EnableSpinWait
#### true/false | bool
If true, uses spin waiting to throttle frame rate. More precise than Yield but uses more CPU. Only applies if EnableYield is false.

### SpinWaitIterations
#### whole number | int
Number of spin iterations per wait cycle when EnableSpinWait is true. Lower values allow higher FPS.
Safe Mode Range: 1 - 1000

### EnableThreadPriority
#### true/false | bool
If true, sets the program's thread priority at launch. Recommended to leave false unless running on a slow computer or alongside heavy audio software.

### ThreadPriority
#### ThreadPriority | enum
Thread priority level when EnableThreadPriority is true.
**Accepted Values:**
```
Lowest
BelowNormal
Normal
AboveNormal
Highest
```

### RendererMode
#### RenderMode | enum
The rendering method used to draw to the console. DirectWrite is fastest for most cases and is the default mode. To change during runtime, unpause then cycle with M key.
**Accepted Values:**
```
PerPixel     
DirtyRect    
RowBatched   
DirectWrite  
```

- **PerPixel:** Renders visual buffer one pixel at a time - slowest
- **DirtyRect:** Renders on changed values in the visual buffer
- **RowBatched:** Renders buffer one row at a time, very fast but only one color can be used
- **DirectWrite:** Renders entire visual buffer at once, has character encoding limitations, recommend to use RasterFont - fastest
- 
### RowBatchColor
#### ConsoleColor | enum
Foreground color used when RendererMode is set to RowBatched. All text on screen will use this single color. Accepts any ConsoleColor value.
**Accepted Values:**
```
Black
DarkBlue
DarkGreen
DarkCyan
DarkRed
DarkMagenta
DarkYellow
Gray
DarkGray
Blue
Green
Cyan
Red
Magenta
Yellow
White
```

## Window
Window settings can be used to modify the program's window behaviour. Via these you can; 
- Force the window to always be on top of other windows 
- Launch full screen 
- Launch at specific sizes or certain positions
- Change the default background color
- 
The background color of the window can also be set here via BackgroundColor

### BackgroundColor
#### ConsoleColor
Background color of the console window. Accepts any ConsoleColor value (same as the acceptable colors above in RowBatchColor).

### DisableCursor
#### true/false | bool
If true, hides the blinking console cursor. Recommended true for clean visuals.

### DisableAppTitle
#### true/false | bool
If true, removes all text from the console window title bar.

### CustomTitle
#### text | string
Custom text for the console window title bar. Leave empty to use "TERMINAL FREQUENCY". Ignored if DisableAppTitle is true.

### DisableTitleBar
#### true/false | bool
Removes the entire title bar from the console window. Use Escape key, F5 or Alt+F4 to close the program.

### DisableScrollBars
#### true/false | bool
Removes horizontal and vertical scroll bars from the console window.

### DisableWindowResize
#### true/false | bool
If true, prevents the user from resizing the console window with the mouse.

### LaunchMaximized
#### true/false | bool
If true, launches the console window maximized. Overrides LaunchInCenter and LaunchAt.

### LaunchInCenter
#### true/false | bool
If true, positions the console in the center of the screen at launch. Ignored if LaunchMaximized is true.

### LaunchAt
#### true/false | bool
If true, launches the console at the coordinates specified by LaunchAtX and LaunchAtY. Ignored if LaunchMaximized is true.

### LaunchAtX
#### whole number | int
Horizontal screen position for the console window when LaunchAt is true. Negative values position relative to the right edge. -1 means use default position.

### LaunchAtY
#### whole number | int
Vertical screen position for the console window when LaunchAt is true. Negative values position relative to the bottom edge. -1 means use default position.

### EnableCustomWindowSize
#### true/false | bool
If true, launches the console at the dimensions specified by CustomWindowWidth and CustomWindowHeight.

### CustomWindowWidth
#### whole number | int
Console width in character columns when EnableCustomWindowSize is true.
Safe Mode Range: 20 - LargestWindowWidth

### CustomWindowHeight
#### whole number | int
Console height in character rows when EnableCustomWindowSize is true.
Safe Mode Range: 10 - LargestWindowHeight

### WindowOpacity
#### whole number | byte
Window transparency level. 0 is fully transparent, 255 is fully opaque.
Safe Mode Range: 0 - 255

### AlwaysOnTop
#### true/false | bool
If true, the console window stays on top of all other windows.

### EnableWindowBlur
#### true/false | bool
If true, applies the Windows acrylic blur effect behind the console window. Ignored if EnableWindowVibrancy is true.

### EnableWindowVibrancy
#### true/false | bool
If true, applies acrylic blur with custom color tinting. Takes priority over EnableWindowBlur.

### WindowVibrancyR
#### whole number | byte
Red component of the vibrancy tint color (0-255). Only applies when EnableWindowVibrancy is true.
Safe Mode Range: 0 - 255

### WindowVibrancyG
#### whole number | byte
Green component of the vibrancy tint color (0-255). Only applies when EnableWindowVibrancy is true.
Safe Mode Range: 0 - 255

### WindowVibrancyB
#### whole number | byte
Blue component of the vibrancy tint color (0-255). Only applies when EnableWindowVibrancy is true.
Safe Mode Range: 0 - 255

### WindowVibrancyA
#### whole number | byte
Alpha (opacity) component of the vibrancy tint color. 0 is fully transparent, 255 is fully opaque. Only applies when EnableWindowVibrancy is true.
Safe Mode Range: 0 - 255

### EnableClickThrough
#### true/false | bool
If true, mouse clicks pass through the console to windows behind it. Pairs well with WindowOpacity and AlwaysOnTop for overlay setups.

### EnableFlashOnBeat
#### true/false | bool
If true, the console window border flashes when a volume spike (beat) is detected.

### FlashOnBeatCount
#### whole number | int
Number of times the window flashes per beat when EnableFlashOnBeat is true.
Safe Mode Range: 1 - 10

### DefaultColors
#### ConsoleColor[]
Array of default ConsoleColor values used throughout the program for cycling and fallback colors. Accepts any ConsoleColor values in any order. Must contain at least one entry.

## AudioCapture
Specific audio parameters can be set here, including; 
- Selecting default audio device 
- Enabling microphone input,
- Adjusting calculated volume when using at low volume 
- Ignore audio data below a certain threshold.


### SpecifyAudioDevice
#### true/false | bool
If true, prompts the user to select an audio capture device from a list at program launch. If false, automatically captures the system audio output.

### AudioSampleResolution
#### whole number | int
Bytes per audio sample. Typically 4 (32-bit float) for WASAPI loopback. 2 for 16-bit audio. Determined by the audio device.
Safe Mode Range: 2 - 4

### RmsMultiplier
#### decimal | float
Scales the raw audio volume to make quiet audio appear louder or loud audio quieter. Increase this value if visuals are not reacting enough at your normal listening volume. Decrease if visuals are too sensitive. Can be used to spoof louder audio from a quiet source.
Safe Mode Range: 10.0 - 500.0

### NoiseGateFloor
#### decimal | float
Audio below this level is treated as silence. Higher values cut out more background noise and device hum but may miss quiet sounds. Lower values keep quiet audio but may trigger visuals from static or humming.
Safe Mode Range: 0.0 - 1.0

### SmoothingFactorExisting
#### decimal | float
Controls how quickly the volume level changes. Higher values make the volume smoother and less jittery but slower to react. Must be paired with SmoothingFactorIncoming so the two values add up to 1.0.
Safe Mode Range: 0.5 - 0.95

### SmoothingFactorIncoming
#### decimal | float
Controls how much new audio data influences the volume level. Higher values make the volume react faster but may appear jittery. Must be paired with SmoothingFactorExisting so the two values add up to 1.0.
Safe Mode Range: 0.05 - 0.5

### PeakTrackingMinimum
#### decimal | float
Minimum volume level that can be tracked as a peak. Prevents low background noise from being recorded as a peak.
Safe Mode Range: 0.05 - 0.3

### PeakDecayFactor
#### decimal | float
How quickly the peak volume decays over time. Higher values hold the peak longer for more dramatic visual effects. Lower values make the peak drop faster for more responsive visuals.
Safe Mode Range: 0.001 - 0.1

### SpikeVolumeMinimum
#### decimal | float
Minimum volume level required for a spike (beat) to be detected. Lower values make beat detection more sensitive but may trigger on non-beat sounds.
Safe Mode Range: 0.01 - 0.2

### SpikeRatio
#### decimal | float
How much louder than the average volume a spike must be to trigger. Lower values are more sensitive (1.0 = any increase triggers). Higher values require a more pronounced beat.
Safe Mode Range: 1.0 - 2.5

## FFT
FFT settings can be used to adjust frequency analysis for certain visuals that use frequencies.
Frequency enabled visualizers include:
- Equalizer

### BandCount
#### whole number | int
How many seperate bands the frequency spectrum gets divided into. If an odd number is set, BandCount will clamp to the next highest even number.
Safe Mode Range: 4 - 32

### Sensitivity
#### decimal | float
Can be adjusted if frequency dependent visualizations are too reactive or not reactive enough. The higher the value the more reactive the bandwidths will be.
Safe Mode Range: 0.3 - 5.0

### DedicatedBassBand
#### true/false | bool
If true, the first band in a frequency sensitive visualization is always dedicated to the lowend. The rest of the bandwidths are programatically calculated accordingly.
The first band will always be between HighPass (Hz) and BassCutoff (Hz). Keep this off if lower frequency bands aren't receiving enough data to seem reactive. Always enabled by default.

### HighPass
#### decimal | float
Ignores any frequency data (Hz) **below** the set value. Set to 30Hz by default. Cannot be greater than LowPass or BassCutoff if DedicatedBassBand is enabled.
Safe Mode Range: 20.0 - 50.0

### LowPass
#### decimal | float
Ignores any frequency data (Hz) **above** the set value. Set to 18k Hz by default. Cannot be lower than HighPass or BassCutoff if DedicatedBassBand is enabled.
Safe Mode Range: 16500.0 - 20000.0

### BassCutoff
#### decimal | float
If DedicatedBassBand is enabled, this value is the max amount (Hz) dedicated to the first band. Cannot be lower than HighPass and cannot be greater than LowPass.
Safe Mode Range: 100.0 - 300.0

## Rings
*Sensitivity Type: Volume, Peak, RMS*
The following settings are used to manipulate the output when using the Rings visualizer.

### ReverseMode
#### true/false | bool
If true, rings start at their maximum radius and shrink inward toward the center instead of expanding outward.

### RadiusMin
#### decimal | float
Minimum radius for reverse mode. When a ring shrinks below this size it disappears.
Safe Mode Range: 1.0 - 100.0

### Radius
#### decimal | float
Starting radius of a ring. Lower values start closer to the center.
Safe Mode Range: 1.0 - 100.0

### RadiusMax
#### decimal | float
Maximum radius a ring can reach before being removed. Must be greater than Radius.
Safe Mode Range: 1.0 - 100.0

### Lifetime
#### decimal | float
Lifespan of a ring in normalized units. Lifetime divided by FadeRate equals the number of frames the ring remains visible.
Safe Mode Range: 0.1 - 10.0

### Speed
#### decimal | float
How many character units the ring expands per frame. Higher values expand faster.
Safe Mode Range: 0.1 - 5.0

### FadeRate
#### decimal | float
Amount of life subtracted per frame. Higher values make rings die faster. Cannot be zero.
Safe Mode Range: 0.001 - 0.05

### SolidColor
#### true/false | bool
If true, rings render in a single solid color based on their position in the color gradient. If false, rings display a gradient fade effect from bright to dark as they expand.

### ColorMode
#### RingColorMode | enum
Color scheme for the rings. Rings transition from bright to dark shades as they age. All and Random are not yet implemented.
**Accepted Values:**
```
Light
Dark
Red
Blue
Green
Yellow
RainbowLight
RainbowDark
All
Random
```

### Characters
#### text list | char[]
Characters used to draw the rings. Each character in the array appears at different positions around the ring. Only the first three characters are used; any additional characters are ignored. Example: ['O', 'o', '.'] draws 'O' at cardinal points, 'o' at half-cardinal points, and '.' elsewhere.

### CharRandomizer
#### true/false | bool
If true, randomly selects characters from CharRandomizerCharset instead of using the Characters array.

### CharRandomizerCharset
#### text | string
String of characters to pick from when CharRandomizer is true. Each frame, a random character from this set is chosen for each ring segment.

### Max
#### whole number | int
Maximum number of rings that can appear on screen simultaneously.
Safe Mode Range: 1 - 30

### Segments
#### whole number | int
Number of points that make up each ring. Lower values create blockier rings. Higher values create smoother circles.
Safe Mode Range: 8 - 80

### AmbientSegments
#### whole number | int
Number of points that make up the ambient/crosshair circle. Lower values create blockier ambient circles.
Safe Mode Range: 8 - 40

### AmbientDotInterval
#### whole number | int
Draws a dot every Nth segment on the ambient/crosshair circle. Lower values create a denser ambient circle.
Safe Mode Range: 1 - 10

### AmbientBaseRadius
#### decimal | float
Minimum radius of the ambient/cross circle when no audio is playing.
Safe Mode Range: 1.0 - 15.0

### AmbientVolumeMultiplier
#### decimal | float
How much the normalized volume affects the ambient/crosshair circle radius. Higher values make the ambient circle pulse more dramatically.
Safe Mode Range: 1.0 - 30.0

### AmbientRadiusMax
#### decimal | float
Maximum radius the ambient circle can reach.
Safe Mode Range: 1.0 - 50.0

### YStretch
#### decimal | float
Vertical compression factor to compensate for console character aspect ratio. Values below 0.45 create flatter circles. Values above create taller ovals.
Safe Mode Range: 0.2 - 0.8

### DrawCrosshair
#### true/false | bool
If true, draws a crosshair at the center of the console where rings originate.

### CrosshairColor
#### ConsoleColor
Color of the center crosshair character. Accepts any ConsoleColor value.

### AmbientColor
#### ConsoleColor
Color of the ambient circle dots. Accepts any ConsoleColor value.

### CrosshairChar
#### text | char
Character used for the center crosshair. Accepts a single character only.

### CrosshairCharOuter
#### text | char
Character used for the ambient circle dots. Accepts a single character only.

### Offset
#### whole number | int
Divisor for the center position. Use 2 for true center. Higher values shift the origin.
Safe Mode Range: 1 - 10

### FireworksMode
#### true/false | bool
Not yet implemented. Intended to randomize the ring origin point on each spike.

## Waterfall
*Sensitivity Type: Volume, Peak, RMS*
The following settings are used to manipulate the output when using the Waterfall visualizer.

### Origin
#### VisualizationOrigin | enum
**Accepted Values:**
Edge of the screen where waterfall streams start from. Center defaults to Top in this mode.
```
Top
Bottom
Left
Right
Center
```

### ReverseMode
#### true/false | bool
If true, streams start at the center of the screen and flow outward toward the Origin edge instead of flowing from the edge to the opposite side.

### Mode
#### WaterfallMode | enum
**Accepted Values:**
Controls how streams are directed across the screen.
```
Normal
Clockwise
AntiClockwise
TopBottom
LeftRight
All
```

- **Normal:** Waterfall starts at VisualizationOrigin and ends in the opposite direction
- **Clockwise:** First waterfall starts at VisualizationOrigin, then every subsequent waterfall follows a clockwise order.
- **AntiClockwise:** Same as Clockwise but in reverse order.
- **TopBottom:** Waterfalls shoot from the top and bottom only.
- **LeftRight:** Waterfalls shoot from the left and right only.
- **All:** Waterfalls shoot from all directions simultaneously.
- 
### StartWidthPercent
#### decimal | float
Width of the waterfall stream at its origin point, as a percentage of the console width or height depending on flow direction.
Safe Mode Range: 0.01 - 0.50

### EndWidthPercent
#### decimal | float
Width of the waterfall stream at the end of its life, as a percentage of the console width or height. Must be higher than StartWidthPercent.
Safe Mode Range: 0.40 - 0.95

### Speed
#### decimal | float
How fast the stream progresses across the screen. Higher values cross faster.
Safe Mode Range: 1.0 - 10.0

### FadeRate
#### decimal | float
Life lost per frame where 1.0 is full life. Higher values make streams fade and disappear faster.
Safe Mode Range: 0.001 - 0.05

### MaxStreams
#### whole number | int
Maximum number of waterfall streams visible at once. Higher values use more CPU.
Safe Mode Range: 1 - 50

### Thickness
#### whole number | int
Number of parallel streams spawned per spike. Thickness of 3 creates 3 streams staggered along the flow direction.
Safe Mode Range: 1 - 10

### TriggerThreshold
#### decimal | float
Minimum volume intensity required to spawn a new stream, as a percentage of peak volume.
Safe Mode Range: 0.01 - 0.30

### OnlySpawnOnThreshold
#### true/false | bool
If true, new streams only spawn when volume is above TriggerThreshold. If false, streams can spawn on any volume spike.

### MidpointChange
#### decimal | float
Progress threshold where the character pattern first changes. At this point, the stream transitions from its initial characters to a more broken-up pattern. Values between 0.0 and 1.0 representing percentage of total stream life.
Safe Mode Range: 0.20 - 0.80

### EndpointChange
#### decimal | float
Progress threshold where the character pattern changes a second time. The stream transitions to scattered characters as it nears its end. Must be higher than MidpointChange.
Safe Mode Range: 0.40 - 0.95

### VerticalChars
#### text list | char[]
Characters rendered on vertical streams (Top/Bottom origin). Index 0 appears at the origin, index 1 at the midpoint, and index 2 near the end. Must contain at least 3 characters.

### HorizontalChars
#### text list | char[]
Characters rendered on horizontal streams (Left/Right origin). Index 0 appears at the origin, index 1 at the midpoint, and index 2 near the end. Must contain at least 3 characters.

### CurveIntensityVertical
#### decimal | float
How pronounced the trailing curve effect is for vertical streams. 0 means no curve. 1 means full curve.
Safe Mode Range: 0.0 - 1.0

### CurveIntensityHorizontal
#### decimal | float
How pronounced the trailing curve effect is for horizontal streams. 0 means no curve. 1 means full curve.
Safe Mode Range: 0.0 - 1.0

### CurveChar
#### text | char
Character used for the trailing curve effect behind the main stream. Accepts a single character.

### RainbowMode
#### true/false | bool
If true, each new stream gets a different color without repeating the previous stream's color. Overrides the Color setting.

### RainbowFadeBright
#### decimal | float
Progress point where the stream transitions from white to its assigned rainbow color. Between 0.0 and 1.0. Only applies when RainbowMode is true.
Safe Mode Range: 0.0 - 1.0

### RainbowFadeColor
#### decimal | float
Progress point where the stream transitions from its assigned color to a darker shade. Between 0.0 and 1.0. Only applies when RainbowMode is true.
Safe Mode Range: 0.0 - 1.0

### RainbowFadeDark
#### decimal | float
Progress point where the stream transitions from dark to dark gray. Between 0.0 and 1.0. Only applies when RainbowMode is true.
Safe Mode Range: 0.0 - 1.0

### RainbowFadeDarkGray
#### decimal | float
Progress point where the stream transitions from dark gray to black (invisible). Between 0.0 and 1.0. Only applies when RainbowMode is true.
Safe Mode Range: 0.0 - 1.0

### NormalFadeWhite
#### decimal | float
Progress point where the stream transitions from white to its assigned Color. Between 0.0 and 1.0. Only applies when RainbowMode is false.
Safe Mode Range: 0.0 - 1.0

### NormalFadeGray
#### decimal | float
Progress point where the stream transitions from its assigned Color to a darker shade. Between 0.0 and 1.0. Only applies when RainbowMode is false.
Safe Mode Range: 0.0 - 1.0

### NormalFadeDarkGray
#### decimal | float
Progress point where the stream transitions from dark gray to black (invisible). Between 0.0 and 1.0. Only applies when RainbowMode is false.
Safe Mode Range: 0.0 - 1.0

### Color
#### ConsoleColor
Default color for streams when RainbowMode is false. Accepts any ConsoleColor value.

## Shape
*Sensitivity Type: Volume, RMS*
The following settings are used to manipulate the output when using the Shape visualizer.

### Type
#### ShapeType
The shape to render. All shapes respond to the same volume and layout settings.
**Accepted Values:**
```
Circle
Square
Diamond
TriangleUp    //Triangle faces towards top of window
TriangleDown  //Triangle faces towards bottom of window
Polygon       //Type of polygon can be adjusted via PolygonSide
```

### Layout
#### ShapeLayout
How shapes are arranged on screen. Single draws one centered shape. Concentric draws multiple layered shapes. Count controls how many shapes appear in each layout.
**Accepted Values:**
```
Single
Vertical
Horizontal
Pyramid
Quadrant
Concentric
```

**Concentric:** In this mode, shapes layer inside each other (If using circle, using concentric mode and having a count of 3 or higher would look like a dart board, see ConcentricLayers).
#### Shape Layout Reference

| Layout      | Count 1     | Count 2          | Count 3              | Count 4              |
|-------------|-------------|------------------|----------------------|----------------------|
| Single      | Center      | Center           | Center               | Center               |
| Vertical    | Center      | Stacked vertical | Stacked vertical     | Stacked vertical     |
| Horizontal  | Center      | Side by side     | Side by side         | side by side         |
| Pyramid     | Center      | Center           | 1 top, 2 bottom      | 2 top, 1 bottom      |
| Quadrant    | 4 Centerd   | TL & BR          | TR & BL              | One per quadrant     |
| Concentric  | Center      | 2 layers         | 3 layers             | 4 layers             |


**Notes:**
- Pyramid counts 1-2 fall back to Single layout
- Concentric layers are controlled by ConcentricLayers setting, not Count
- Quadrant count 2 uses top-left and bottom-right corners by default
- Quadrant count 3 uses top-right and bottom-left corners by default

### VolumeSensitivity
#### decimal | float
Scales how much the shape responds to volume changes. 1.0 is full range, 0.5 is half, 0.1 barely moves. Works together with TriggerThreshold to fine-tune responsiveness.
Safe Mode Range: 0.1 - 1.0

### TriggerThreshold
#### decimal | float
Volume below this level is treated as silence. The shape will stay at its minimum size until volume exceeds this threshold.
Safe Mode Range: 0.0 - 1.0

### MaxSizePercent
#### decimal | float
Maximum size of the shape as a percentage of the smallest console dimension. At max volume, the shape reaches this size. Must be higher than MinSizePercent.
Safe Mode Range: 0.02 - 0.99

### MinSizePercent
#### decimal | float
Minimum size of the shape as a percentage of the smallest console dimension. At zero volume, the shape shrinks to this size.
Safe Mode Range: 0.0 - 0.50

### Count
#### whole number | int
Number of shapes to render. Used by all layouts except Concentric (which uses ConcentricLayers). Pyramid layout uses Count differently: 1-2 draws a single shape, 3 draws a pyramid, 4 draws an inverted pyramid.
Safe Mode Range: 1 - 4

### ConcentricLayers
#### whole number | int
Number of concentric layers when Layout is set to Concentric. Each layer is a smaller copy of the outer shape.
Safe Mode Range: 1 - 10

### ConcentricPadding
#### whole number | int
Character spacing between concentric layers. Higher values create more space between each ring.
Safe Mode Range: 1 - 10

### Thickness
#### whole number | int
Outline thickness of the shape in characters. Higher values create thicker borders. Automatically scaled down when multiple shapes are on screen.
Safe Mode Range: 1 - 10

### ThicknessMax
#### whole number | int
Maximum allowed thickness after auto-scaling. Prevents shapes from becoming solid blobs when Thickness is set high.
Safe Mode Range: 1 - 20

### QuadrantCentered
#### true/false | bool
If true and Count is 4, shapes cluster around the center of the screen instead of the corners. Only applies to Quadrant layout.

### QuadrantIndices
#### whole number list | int[]
Manual quadrant positions when Layout is Quadrant. 0 = top-left, 1 = top-right, 2 = bottom-left, 3 = bottom-right. Leave empty for automatic placement based on Count.

### QuadrantGapDivisor
#### whole number | int
Controls the gap between shapes in centered Quadrant mode. Smaller values create wider gaps. Higher values bring shapes closer together.
Safe Mode Range: 5 - 20

### UseCustomColor
#### true/false | bool
If true, each shape uses a different color from the CustomColors array. If false, all shapes use UniformColor.

### UniformColor
#### ConsoleColor
Default color for all shapes when UseCustomColor is false. Accepts any ConsoleColor value.

### CustomColors
#### ConsoleColor[] | list
Array of colors used when UseCustomColor is true. Each shape index uses the corresponding color in this array. Wraps around if there are more shapes than colors.

### ReverseMode
#### true/false | bool
If true, shapes start at their maximum size and shrink inward as volume increases. The shape is largest at silence and smallest at peak volume.

### ReverseVolumeSensitivity
#### decimal | float
Controls how close to the center the shape reaches at max volume in reverse mode. Values closer to 0.0 allow the shape to shrink all the way to the center.
Safe Mode Range: 0.01 - 0.20

### SmoothMode
#### true/false | bool
If true, the shape smoothly transitions between sizes. If false, the shape snaps instantly to the volume-based size.

### LerpFactor
#### decimal | float
Smoothing speed when SmoothMode is true. Higher values snap faster. 1.0 is instant (same as SmoothMode off).
Safe Mode Range: 0.01 - 1.0

### Character
#### text | char
Character used to draw the shape outline. Accepts a single character. Block characters work well.

### VerticalStack
#### true/false | bool
If true and Layout is Vertical with Count of 2, shapes stack vertically. If false, they stack horizontally. Applies to Vertical and Horizontal layouts.

### CircleSegmentDensity
#### decimal | float
How many points make up the circle outline relative to its circumference. 1.0 is one point per radian. Higher values create smoother circles but use more CPU.
Safe Mode Range: 0.3 - 1.5

### CircleMinSegments
#### whole number | int
Minimum number of points for a circle. Values below 12 may result in visible polygons instead of smooth circles.
Safe Mode Range: 6 - 20

### CircleMaxSegments
#### whole number | int
Maximum number of points for a circle. Limits CPU usage on very large circles.
Safe Mode Range: 60 - 200

### SquareWidthRatio
#### decimal | float
Width multiplier for squares. 1.0 creates a perfect square. 0.5 creates a half-width rectangle. 2.0 creates a double-width rectangle.
Safe Mode Range: 0.1 - 5.0

### SquareHeightRatio
#### decimal | float
Height multiplier for squares. 1.0 creates a perfect square. 0.5 creates a half-height rectangle. 2.0 creates a double-height rectangle.
Safe Mode Range: 0.1 - 5.0

### TriangleSideMultiplier
#### decimal | float
Side length of triangles relative to the shape radius. Higher values create wider triangles.
Safe Mode Range: 0.5 - 4.0

### TriangleHeightMultiplier
#### decimal | float
Height of triangles relative to their side length. 0.87 approximates an equilateral triangle. Adjust for different proportions.
Safe Mode Range: 0.1 - 2.0

### TriangleAspectCorrection
#### decimal | float
Console character aspect ratio correction for triangles. Compensates for characters being taller than wide.
Safe Mode Range: 0.1 - 1.0

### PyramidRowSpacing
#### decimal | float
Space between rows in the Pyramid layout as a fraction of screen height. Higher values create more vertical space.
Safe Mode Range: 0.08 - 0.30

### PolygonSides
#### whole number | int
Number of sides for the Polygon shape type. Common values are 5 (pentagon), 6 (hexagon), 8 (octagon), 10 (decagon). Very high values approach a circle.
Safe Mode Range: 5 - 12

### FillMode
#### true/false | bool
If true, fills the interior of the shape with the shape's color. The outline still renders on top.

### FillSpacing
#### whole number | int
Spacing between fill characters. 0 creates solid fill. 1 fills every other pixel. Higher values create sparse dithering effects.
Safe Mode Range: 0 - 3

## EqualizerSettings
*Sensitivity Type: Frequency Spectrum**
The following settings are used to manipulate the output when using the Equalizer visualizer.

### Origin
#### VisualizationOrigin
Where the Equalizer is positioned in the window. `Center`` will position an Equalizer in the center of the window with the bands moving the same both ways (like a waveform).
**Accepted Values:**
```
Top
Right
Bottom
Left
Center
```

### ColorMode
#### EqColorMode | enum
**Accepted Values:**
```
Uniform  //Every band on the screen is the same colour, configured via UniformColor
Pattern  //Cycles through ColorPattern.
Gradient //Each band is 3 seperate colours based on volume, can be set via GradientColors.
```

### UniformColor
#### ConsoleColor
Color of all bands if using Uniform color mode.

### ColorPattern
#### list of ConsoleColors | ConsoleColor[]
When using Pattern color mode, cycles through this list for each band.
(ex): Colors are "Red", "Green", "Blue". There are 8 bands. Bands 1, 4 and 7 will be Red. Bands 2, 5, 8 will be Green. Bands 3 and 6 will be Blue.
Ignores any colors that would exceed the count of bands (via FFT settings/BandCount)

### GradientColors
#### list of ConsoleColors[] | ConsoleColor[]
Accepts 3 colors to be used when in Gradient color mode. In this mode, each band is rendered with a volume-sensitive color:
- Color 1 represents lower volume.
- Color 2 represents mid-volume.
- Color 3 represents high-volume peaks and clips.

If more than 3 colors provided, only the first 3 will ever be used.

### SolidBands
#### true/false | bool
If true, fills the inside of band with BandCharacter. If false, only renders outline of the band.

### SmoothMode
#### true/false | bool
If true, uses LerpFactor to prevent the band visuals from snapping to the next value.

### LerpFactor
#### decimal | float
Can be adjusted to determine how much smoothing is applied. Cannot be greater than 1.0, which is essentially the same as having smoothing disabled.
Safe Mode Range: 0.01 - 1.0

### Direction
#### EqDirection | enum
What order the Equalizer is rendered in.

**AcceptedValues:**
```
LowToHigh
HighToLow
Mirror
```

- **LowToHigh:** Lower frequencies to the left, higher to the right.
- **HighToLow:** Opposite of LowToHigh
- **Mirror:** The first band is the same as the last band. Second is the same as second last, so on and so forth. Cuts the FFT BandCount (see FFT) in half for the effect *(If BandCount is 8, only 4 bands of data are registered)*

### BandCharacter
#### text | char
Text character used when rendering bands, including the fill via SolidBands.

### BandSpacing
#### whole number | int
Increase or decrease to adjust space between bands. Increasing doesn't actually create space but rather shrinks the bands to fit the width/height of the window.

### MaxBandHeightPercent
#### decimal | float
Decimal percentage of how high a band can get relative to 100% of available space. Cannot be greater than 1.0 (100%) or lower than MinBandHeightPercent.
Safe Mode Range: 0.5 - 1.0

### MaxBandHeightPercent
#### decimal | float
Decimal percentage of how high a band can get relative to 0% of available space, where 0% is not visible at all. Cannot be lower than 0 or greater than MaxBandHeightPercent.
Safe Mode Range: 0.00 - 0.49
