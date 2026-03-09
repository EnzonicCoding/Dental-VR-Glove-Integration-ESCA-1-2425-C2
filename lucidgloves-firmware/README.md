# LucidGloves Firmware — Dental VR Glove Integration

This folder contains the Arduino firmware for the DIY VR glove hardware used in the Dental VR simulation. It is based on the [LucidGloves](https://github.com/LucidVR/lucidgloves) open-source project and adapted for use with the Dental VR training application.

## Hardware

| Component | Details |
|-----------|---------|
| Microcontroller | ESP32 (DevKit v1 or equivalent) |
| Flex sensors | 5× resistive flex sensors (one per finger) |
| Haptic motors | 5× coin/ERM vibration motors driven via transistor |
| Communication | BLE (default) or USB Serial |

## Folder structure

```
lucidgloves-firmware/
└── lucidgloves-firmware/
    ├── lucidgloves-firmware.ino   # Main Arduino sketch
    └── Config.h                   # Pin mapping & calibration settings
```

## Flashing the firmware

1. Install the [Arduino IDE](https://www.arduino.cc/en/software) (2.x recommended).
2. Add ESP32 board support: `File → Preferences → Additional Boards Manager URLs`:
   ```
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
3. Install **ESP32 by Espressif Systems** via `Tools → Board → Boards Manager`.
4. Open `lucidgloves-firmware/lucidgloves-firmware.ino` in the Arduino IDE.
5. Edit `Config.h` to match your wiring (pin numbers and calibration values).
6. Select your board (`ESP32 Dev Module`) and COM port, then click **Upload**.

## Communication protocol

### Glove → Unity (finger curl data)
Sent every ~4 ms as a newline-terminated string:
```
A<0-100>B<0-100>C<0-100>D<0-100>E<0-100>\n
```
Letters map to fingers: `A`=Thumb, `B`=Index, `C`=Middle, `D`=Ring, `E`=Pinky.  
Values are curl percentages (0 = fully open, 100 = fully closed).

### Unity → Glove (force-feedback / haptic commands)
```
FFB A<0-100> B<0-100> C<0-100> D<0-100> E<0-100>
```
Example — pulse the index finger at 75% intensity:
```
FFB A0 B75 C0 D0 E0
```

## Integration with Unity (Dental VR)

The Unity-side integration lives in the main project's C# scripts. The glove driver reads BLE data and maps finger curl values to XR controller inputs. Haptic commands are sent back from the `SyringeHaptics.cs` and `ToolDebugger.cs` scripts whenever the simulated tool contacts a tooth or triggers force feedback.

## Calibration

Edit `Config.h`:

```cpp
#define FLEX_MIN  1800  // ADC value — hand fully open
#define FLEX_MAX  3200  // ADC value — hand fully closed
```

Run a calibration sketch (or use Serial Monitor) to find the correct min/max ADC readings for your specific flex sensors before using the glove in the simulation.
