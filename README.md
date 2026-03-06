# Dental-VR-Glove-Integration

Unity integration of **LucasVRTech's LucidGloves** (OpenGloves / LucidVR) into a
Dental Simulation VR project.  The gloves provide real-time **finger tracking** via
SteamVR's skeletal input system and bidirectional **force-feedback** over serial for
tactile simulation of dental procedures (drilling, probing, extractions, etc.).

> **⚠️ Meta Quest 2 Tracking Lost After Installing SteamVR?**
> See **[Section 0 — Restoring Meta Quest Tracking](#0-restoring-meta-quest-2-tracking-after-adding-steamvr)** first.

---

## Table of Contents

0. [Restoring Meta Quest 2 Tracking After Adding SteamVR](#0-restoring-meta-quest-2-tracking-after-adding-steamvr)
1. [Prerequisites](#1-prerequisites)
2. [Hardware Setup](#2-hardware-setup)
3. [Software Setup](#3-software-setup)
4. [Unity Project Setup](#4-unity-project-setup)
5. [Scene Wiring Guide](#5-scene-wiring-guide)
6. [Script Reference](#6-script-reference)
7. [SteamVR Input Bindings](#7-steamvr-input-bindings)
8. [Calibration](#8-calibration)
9. [Troubleshooting](#9-troubleshooting)

---

## 0. Restoring Meta Quest 2 Tracking After Adding SteamVR

### Why This Happens

Unity's XR Plugin Management system only allows **one active XR provider per
build target**.  When the SteamVR Unity Plugin (Valve's `com.valvesoftware.unity.openvr`
package) is imported, it can register itself as the Windows XR provider and
override the Oculus XR Plugin, which is what the Meta Quest uses for headset and
controller tracking.

Additionally, the old `HapticTest.cs` script called `OVRInput.SetControllerVibration`
— an Oculus-SDK-specific API — which silently fails (and can cause compile errors)
once the Oculus XR Plugin is no longer active.

### What Was Fixed in This PR

| File | Change |
|---|---|
| `HapticTest.cs` | Replaced `OVRInput.SetControllerVibration` with the platform-agnostic `UnityEngine.XR.InputDevice.SendHapticImpulse` API |
| `GloveInputManager.cs` | Added `IsGlovesAvailable` runtime check — all glove API calls become no-ops when SteamVR is not the active XR runtime |
| `FingerTrackingController.cs` | Component self-disables when SteamVR is not active, preventing spurious errors |

### How to Restore Meta Quest Tracking in Unity Editor

Follow these steps **in the Unity Editor** to ensure the Quest headset is properly
tracked regardless of whether SteamVR is also installed:

#### Step 1 — Open XR Plugin Management

`Edit → Project Settings → XR Plug-in Management`

#### Step 2 — Configure the Android tab (Quest Standalone builds)

1. Select the **Android** tab (🤖 icon).
2. Check ✅ **Oculus** (or **OpenXR** with the Meta Quest Feature Group enabled).
3. Uncheck any SteamVR / OpenVR entries on the Android tab — they serve no purpose there.

#### Step 3 — Configure the Windows tab (PC VR builds with gloves)

1. Select the **Windows** tab (🖥️ icon).
2. Choose **one** of the following options:
   - **Option A — OpenVR (SteamVR plugin)** ← for LucidGloves via SteamVR
     - Check ✅ **OpenVR** (Valve SteamVR plugin).
     - Uncheck ❌ Oculus on the Windows tab.
     - Run via Quest Link / Air Link so the Quest headset connects through SteamVR.
   - **Option B — OpenXR** ← recommended for a stable, future-proof setup
     - Check ✅ **OpenXR**.
     - Under `OpenXR Feature Groups → Windows`, enable **Oculus Touch Controller Profile** (for Quest controllers).
     - Disable the Meta Quest Feature Group on the Windows tab — it is only for Android.
     - Set SteamVR as the system OpenXR runtime (`SteamVR → Settings → OpenXR tab → Set SteamVR as OpenXR Runtime`).
     - The OpenGloves driver will route glove data through this runtime.

#### Step 4 — Disable `HapticTest.cs` in your scene (or leave it — it's now fixed)

The `HapticTest.cs` script was rewritten to use the XR Interaction Toolkit
haptics API, so it will work with any backend.  You can safely leave it in the
scene or remove it if you no longer need it.

#### Summary Table

| Build Target | XR Provider | Use Case |
|---|---|---|
| Android | Oculus XR Plugin **or** OpenXR + Meta Quest Feature Group | Quest standalone, no gloves |
| Windows | OpenVR (SteamVR plugin) | PC VR + LucidGloves, Quest via SteamVR |
| Windows | OpenXR (SteamVR as runtime) | PC VR + LucidGloves, Quest via SteamVR — more portable |

> **Rule of thumb:** Only one XR plugin should be checked per build target.
> Never have both Oculus and OpenVR checked at the same time on the same platform tab.

---

## 1. Prerequisites

| Requirement | Version / Notes |
|---|---|
| Unity | 2021.3 LTS or newer (URP / Built-in both supported) |
| SteamVR Unity Plugin | [Asset Store](https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647) or [GitHub](https://github.com/ValveSoftware/steamvr_unity_plugin) |
| Steam + SteamVR | Latest release |
| OpenGloves Driver | [Steam page](https://store.steampowered.com/app/1574050/OpenGloves/) or [GitHub](https://github.com/LucidVR/opengloves-driver) |
| LucidGloves Firmware | [LucidVR/lucidgloves](https://github.com/LucidVR/lucidgloves) (Prototype 4+ recommended) |
| Arduino IDE / PlatformIO | For flashing firmware to the ESP32/Arduino |
| VR Headset | Any SteamVR-compatible headset |

---

## 2. Hardware Setup

1. **Assemble the gloves** following the
   [LucidGloves build guide](https://github.com/LucidVR/lucidgloves/wiki).
   Prototype 4 (ESP32-based) is recommended for wireless + haptics support.

2. **Flash the firmware**:
   - Open the firmware project in the Arduino IDE / PlatformIO.
   - Select the correct board (ESP32 or Arduino Nano).
   - Set `COMMUNICATION_PROTOCOL` to `SERIAL` or `BLUETOOTH` in `lucidgloves-firmware/src/Config.h`.
   - Enable `FORCE_FEEDBACK` if your build includes servo motors.
   - Upload to both the left and right gloves.

3. **Pair / connect the gloves**:
   - *Serial*: Connect via USB and note the assigned COM port (e.g. `COM3`, `COM5`).
   - *Bluetooth*: Pair each glove in Windows Bluetooth settings.

---

## 3. Software Setup

1. **Install Steam and SteamVR** on your PC.

2. **Install the OpenGloves driver**:
   - Subscribe on Steam **or** download the release from
     [GitHub](https://github.com/LucidVR/opengloves-driver/releases) and install
     manually via `vrpathreg adddriver`.

3. **Configure the driver**:
   - Launch SteamVR → Settings → OpenGloves.
   - Set the correct COM port(s) and baud rate (default **115200**) for each glove.
   - Verify that both gloves appear as tracked devices in the SteamVR status window.

---

## 4. Unity Project Setup

### 4.1 Import the SteamVR Plugin

1. In Unity open **Window → Package Manager** (or the Asset Store).
2. Import the **SteamVR Plugin** package.
3. Accept the default input-system upgrade prompt when it appears.
4. Unity will generate `Assets/SteamVR_Input/` and the default action set files.

### 4.2 Add the Glove Integration Scripts

All integration scripts live under:

```
Assets/
  Scripts/
    GloveIntegration/
      GloveInputManager.cs          ← Central singleton manager
      FingerTrackingController.cs   ← Per-hand finger-curl reader
      HandAnimationController.cs    ← Drives finger rig from curl values
      HapticFeedbackController.cs   ← Serial force-feedback sender
      GloveCalibration.cs           ← Per-finger sensor calibration
    Dental/
      DentalToolInteraction.cs      ← Grab / release with haptic response
      DentalDrillHaptics.cs         ← Drill vibration pattern
      DentalProcedureManager.cs     ← High-level procedure haptics API
```

### 4.3 Copy the SteamVR Action Definitions

Copy the files from `Assets/StreamingAssets/SteamVR/` into the SteamVR input
folder that was generated for your project:

```
Assets/StreamingAssets/SteamVR/
  actions.json                            ← Action set definition
  default_bindings_lucidgloves_left.json
  default_bindings_lucidgloves_right.json
```

Then in Unity open **Window → SteamVR Input** and click **Save and generate**.

---

## 5. Scene Wiring Guide

### Step 1 — Glove Manager

1. Create an empty GameObject named `GloveManager`.
2. Add the **`GloveInputManager`** component.
3. It calls `DontDestroyOnLoad` automatically so it persists across scenes.

### Step 2 — Left Hand

1. Inside your VR Rig's left-hand anchor:
   - Add a **`SteamVR_Behaviour_Pose`** component, set `Input Source = LeftHand`.
   - Add a **`SteamVR_Behaviour_Skeleton`** component, set `Input Source = LeftHand`.
   - Add a **`FingerTrackingController`** component:
     - Drag the `SteamVR_Behaviour_Skeleton` into **Hand Skeleton**.
     - Set **Input Source** to `LeftHand`.
   - (Optional) Add **`GloveCalibration`** and assign it to the tracker.
   - Add a **`HapticFeedbackController`** component:
     - Set **Port Name** to your left glove's COM port (e.g. `COM3`).
   - Add a **`HandAnimationController`** component:
     - Assign the `FingerTrackingController` reference.
     - Fill in the five finger bone-chain references from your hand rig.
2. Back on `GloveManager`, assign the left-hand `FingerTrackingController` and
   `HapticFeedbackController` to the corresponding fields.

### Step 3 — Right Hand

Repeat Step 2 for the right hand, using `RightHand` as the input source and the
right glove's COM port.

### Step 4 — Dental Tools

For each interactable tool (drill, mirror, probe, scaler, …):

1. Add a **`Rigidbody`** component (isKinematic can start as false).
2. Add a **`DentalToolInteraction`** component:
   - Set **Tool Name**, **Grab Threshold**, and haptic values.
3. For the drill specifically, also add **`DentalDrillHaptics`** and configure
   speed and pulse settings.

### Step 5 — Procedure Manager

1. Create an empty GameObject named `ProcedureManager`.
2. Add the **`DentalProcedureManager`** component.
3. Assign the `GloveInputManager` reference.
4. Call its methods from your procedure scripts:
   ```csharp
   procedureManager.TriggerProbeTap(SteamVR_Input_Sources.RightHand);
   procedureManager.StartExtractionFeedback(SteamVR_Input_Sources.RightHand);
   procedureManager.TriggerImpressionSeat();
   ```

---

## 6. Script Reference

### `GloveInputManager`

| Member | Description |
|---|---|
| `Instance` | Singleton accessor |
| `IsGlovesAvailable` | True only when SteamVR/OpenVR is the active XR runtime; all other calls are no-ops when false |
| `GetFingerCurls(hand)` | Returns `float[5]` curl values (0–1) for the given hand |
| `SendHapticPulse(hand, finger, strength, duration)` | Single-finger haptic pulse |
| `SendFullHandHaptics(hand, strength, duration)` | All-finger haptic burst |
| `IsHandTracked(hand)` | True when SteamVR reports the device as tracked |
| `OnGlovesConnected` | Event fired when both gloves are connected |
| `OnGlovesDisconnected` | Event fired when a glove disconnects |

### `FingerTrackingController`

| Member | Description |
|---|---|
| `FingerCurls` | `float[5]` — [thumb, index, middle, ring, pinky], 0–1 |
| `IsTracked` | SteamVR tracking state |
| `GetFingerCurl(index)` | Single finger curl |
| `IsFingerBent(index, threshold)` | True if curl ≥ threshold |
| `IsGrabbing(threshold)` | Four-finger grab gesture |
| `IsPinching(bentT, openT)` | Thumb + index pinch gesture |

### `HapticFeedbackController`

| Member | Description |
|---|---|
| `OpenPort()` / `ClosePort()` | Manual serial connection management |
| `IsConnected` | True when the serial port is open |
| `SendForceFeedback(finger, strength, duration)` | Single-finger FFB |
| `SendAllFingersFeedback(forces[], duration)` | Multi-finger FFB |
| `ReleaseAll()` | Immediately zeros all finger forces |

### `GloveCalibration`

| Member | Description |
|---|---|
| `Apply(fingerIndex, rawValue)` | Maps raw sensor value to calibrated 0–1 range |
| `RecordOpenPose(rawCurls)` | Record open-hand reference |
| `RecordClosedPose(rawCurls)` | Record fist reference |
| `ResetCalibration()` | Reset to identity passthrough |

---

## 7. SteamVR Input Bindings

The files in `Assets/StreamingAssets/SteamVR/` define a custom action set
`/actions/dental` with the following actions:

| Action | Type | Description |
|---|---|---|
| `SkeletonLeft` / `SkeletonRight` | skeleton | Full hand skeleton from OpenGloves driver |
| `GrabLeft` / `GrabRight` | boolean | Grip button maps to grab gesture |
| `PinchLeft` / `PinchRight` | boolean | Trigger maps to pinch gesture |
| `PoseLeft` / `PoseRight` | pose | Raw hand pose / position |
| `HapticLeft` / `HapticRight` | vibration | SteamVR haptic output (supplementary) |

After importing these files, re-open **Window → SteamVR Input → Save and generate**
to regenerate the C# wrapper classes.

---

## 8. Calibration

1. Enter Play Mode.
2. **Open pose**: Hold your hand completely flat and open, then call:
   ```csharp
   var tracker = /* your FingerTrackingController */;
   var calibration = /* your GloveCalibration */;
   calibration.RecordOpenPose(tracker.FingerCurls);
   ```
3. **Closed pose**: Make a tight fist, then call:
   ```csharp
   calibration.RecordClosedPose(tracker.FingerCurls);
   ```
4. Calibration values are serialised in the component so you can store them as
   a Unity Preset or copy them into the Scene.

Alternatively, use the OpenGloves companion app (installed alongside the driver)
for a hardware-level calibration pass before launching Unity.

---

## 9. Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| **Meta Quest headset tracking lost after adding SteamVR** | SteamVR plugin replaced the Oculus XR Plugin as the active XR provider | See [Section 0](#0-restoring-meta-quest-2-tracking-after-adding-steamvr). In `Project Settings → XR Plug-in Management → Windows`, switch from OpenVR to Oculus (or OpenXR with Oculus Touch profile) |
| **OVRInput compile error / `OVRInput` not found** | Oculus XR Plugin removed or inactive | `HapticTest.cs` now uses platform-agnostic `UnityEngine.XR` APIs — no further action needed |
| Gloves not tracked in SteamVR | Driver not installed / COM port wrong | Re-install OpenGloves driver; verify COM port in driver settings |
| Finger curls always 0 | Skeleton action not bound | Open SteamVR Input, check bindings for `lucidgloves` controller type |
| No force feedback | Wrong COM port in `HapticFeedbackController` | Check Device Manager → Ports; update `portName` field |
| Jumpy finger animations | Low-quality potentiometers / electrical noise | Increase `smoothingSpeed` in `HandAnimationController` or add a deadband in `GloveCalibration` |
| "SteamVR is not initialised" warning in Editor | SteamVR not running | Start SteamVR before entering Play Mode, or ignore — gloves degrade gracefully |
| Serial port access denied | Port already open by driver | Close the OpenGloves driver config window before running Unity |
| `GloveInputManager.IsGlovesAvailable` is false | Correct — the active runtime is Meta/Oculus, not SteamVR | Expected behaviour on a Quest build; gloves only activate when running via SteamVR on PC |

---

## References

- [LucidVR/lucidgloves — Firmware & Hardware](https://github.com/LucidVR/lucidgloves)
- [LucidVR/opengloves-driver — SteamVR Driver](https://github.com/LucidVR/opengloves-driver)
- [Valve SteamVR Unity Plugin](https://github.com/ValveSoftware/steamvr_unity_plugin)
- [SteamVR Input Documentation](https://partner.steamgames.com/doc/features/steamvr/input)
- [OpenGloves on Steam](https://store.steampowered.com/app/1574050/OpenGloves/)
- [Unity XR Plugin Management](https://docs.unity3d.com/Manual/com.unity.xr.management.html)
- [Meta Quest OpenXR Project Setup](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@1.0/manual/project-setup.html)
- [Unity XR InputDevice Haptics API](https://docs.unity3d.com/ScriptReference/XR.InputDevice.SendHapticImpulse.html)
