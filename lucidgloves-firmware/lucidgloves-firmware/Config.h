// Config.h
// Hardware and communication configuration for LucidGloves Dental VR Integration
// Edit this file to match your wiring and preferred communication method.

#pragma once

// ---------------------------------------------------------------------------
// Communication mode — uncomment exactly ONE option
// ---------------------------------------------------------------------------
#define COMMUNICATION_BLE       // Bluetooth Low Energy (ESP32 default)
// #define COMMUNICATION_SERIAL // USB Serial (for wired debugging)

// ---------------------------------------------------------------------------
// Device identity (used as BLE device name)
// ---------------------------------------------------------------------------
#define DEVICE_NAME "LucidGloves_Dental_L"  // Change to _R for right hand

// ---------------------------------------------------------------------------
// Number of fingers tracked
// ---------------------------------------------------------------------------
#define NUM_FINGERS 5

// ---------------------------------------------------------------------------
// Flex sensor analog input pins (ESP32 ADC1 pins recommended)
// Order: Thumb, Index, Middle, Ring, Pinky
// ---------------------------------------------------------------------------
const int FLEX_PINS[NUM_FINGERS] = {36, 39, 34, 35, 32};

// ---------------------------------------------------------------------------
// Haptic motor PWM output pins
// Order: Thumb, Index, Middle, Ring, Pinky
// ---------------------------------------------------------------------------
const int HAPTIC_PINS[NUM_FINGERS] = {13, 12, 14, 27, 26};

// ---------------------------------------------------------------------------
// Flex sensor calibration
// Adjust FLEX_MIN / FLEX_MAX to match your physical sensors:
//   FLEX_MIN = ADC reading when finger is fully straight (open hand)
//   FLEX_MAX = ADC reading when finger is fully curled (closed fist)
// ---------------------------------------------------------------------------
#define FLEX_MIN  1800
#define FLEX_MAX  3200

// ---------------------------------------------------------------------------
// Serial baud rate (used when COMMUNICATION_SERIAL is selected)
// ---------------------------------------------------------------------------
#define SERIAL_BAUD_RATE 115200

// ---------------------------------------------------------------------------
// Main loop delay (milliseconds)
// Lower = more responsive haptics / higher data rate; higher = power savings.
// ---------------------------------------------------------------------------
#define LOOP_DELAY_MS 4
