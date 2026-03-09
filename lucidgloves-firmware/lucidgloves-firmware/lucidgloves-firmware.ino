// lucidgloves-firmware.ino
// LucidGloves firmware adapted for Dental VR Glove Integration
// Reads flex sensors on each finger, sends data over BLE/Serial,
// and drives haptic feedback motors based on commands received from Unity.

#include "Config.h"

#include <Arduino.h>

#if defined(COMMUNICATION_BLE)
  #include <BLEDevice.h>
  #include <BLEServer.h>
  #include <BLEUtils.h>
  #include <BLE2902.h>

  BLEServer* pServer = nullptr;
  BLECharacteristic* pTxCharacteristic = nullptr;
  BLECharacteristic* pRxCharacteristic = nullptr;
  bool deviceConnected = false;

  #define SERVICE_UUID        "6e400001-b5a3-f393-e0a9-e50e24dcca9e"
  #define CHARACTERISTIC_UUID_RX "6e400002-b5a3-f393-e0a9-e50e24dcca9e"
  #define CHARACTERISTIC_UUID_TX "6e400003-b5a3-f393-e0a9-e50e24dcca9e"

  class ServerCallbacks : public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) override { deviceConnected = true; }
    void onDisconnect(BLEServer* pServer) override {
      deviceConnected = false;
      pServer->startAdvertising();
    }
  };

  class RxCallbacks : public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic* pCharacteristic) override {
      std::string value = pCharacteristic->getValue();
      if (value.length() > 0) {
        handleCommand(String(value.c_str()));
      }
    }
  };
#endif

// --- Finger curl values (0-4095 for ESP32 ADC) ---
int fingerCurl[NUM_FINGERS] = {0, 0, 0, 0, 0};

// --- Haptic motor state ---
bool hapticActive[NUM_FINGERS] = {false, false, false, false, false};
float hapticIntensity[NUM_FINGERS] = {0.0f, 0.0f, 0.0f, 0.0f, 0.0f};

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------
void setup() {
  Serial.begin(SERIAL_BAUD_RATE);

  // Flex sensor pins (analog input)
  for (int i = 0; i < NUM_FINGERS; i++) {
    pinMode(FLEX_PINS[i], INPUT);
  }

  // Haptic motor pins (PWM output)
  for (int i = 0; i < NUM_FINGERS; i++) {
    pinMode(HAPTIC_PINS[i], OUTPUT);
    analogWrite(HAPTIC_PINS[i], 0);
  }

#if defined(COMMUNICATION_BLE)
  BLEDevice::init(DEVICE_NAME);
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new ServerCallbacks());

  BLEService* pService = pServer->createService(SERVICE_UUID);

  pTxCharacteristic = pService->createCharacteristic(
      CHARACTERISTIC_UUID_TX, BLECharacteristic::PROPERTY_NOTIFY);
  pTxCharacteristic->addDescriptor(new BLE2902());

  pRxCharacteristic = pService->createCharacteristic(
      CHARACTERISTIC_UUID_RX, BLECharacteristic::PROPERTY_WRITE);
  pRxCharacteristic->setCallbacks(new RxCallbacks());

  pService->start();
  pServer->getAdvertising()->start();
  Serial.println("[LucidGloves] BLE advertising started as: " DEVICE_NAME);
#else
  Serial.println("[LucidGloves] Serial communication mode active.");
#endif
}

// ---------------------------------------------------------------------------
// Main loop
// ---------------------------------------------------------------------------
void loop() {
  readFlexSensors();
  applyHaptics();
  sendFingerData();

#if !defined(COMMUNICATION_BLE)
  // Poll for haptic commands on Serial
  if (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    handleCommand(cmd);
  }
#endif

  delay(LOOP_DELAY_MS);
}

// ---------------------------------------------------------------------------
// Read flex sensors for each finger
// ---------------------------------------------------------------------------
void readFlexSensors() {
  for (int i = 0; i < NUM_FINGERS; i++) {
    int raw = analogRead(FLEX_PINS[i]);
    // Map raw ADC value to 0–100 curl percentage
    fingerCurl[i] = map(raw, FLEX_MIN, FLEX_MAX, 0, 100);
    fingerCurl[i] = constrain(fingerCurl[i], 0, 100);
  }
}

// ---------------------------------------------------------------------------
// Apply PWM to haptic motors based on current hapticIntensity values
// ---------------------------------------------------------------------------
void applyHaptics() {
  for (int i = 0; i < NUM_FINGERS; i++) {
    if (hapticActive[i]) {
      int pwmValue = (int)(hapticIntensity[i] * 255.0f);
      analogWrite(HAPTIC_PINS[i], constrain(pwmValue, 0, 255));
    } else {
      analogWrite(HAPTIC_PINS[i], 0);
    }
  }
}

// ---------------------------------------------------------------------------
// Send finger curl data to Unity (CSV: "A%dB%dC%dD%dE%d\n")
// Format mirrors the original LucidGloves protocol so the Unity driver
// (OpenGloves / custom GloveInputDriver) can parse it without modification.
// ---------------------------------------------------------------------------
void sendFingerData() {
  char buf[64];
  int len = snprintf(buf, sizeof(buf), "A%dB%dC%dD%dE%d\n",
                     fingerCurl[0], fingerCurl[1], fingerCurl[2],
                     fingerCurl[3], fingerCurl[4]);

#if defined(COMMUNICATION_BLE)
  if (deviceConnected) {
    pTxCharacteristic->setValue((uint8_t*)buf, (size_t)len);
    pTxCharacteristic->notify();
  }
#else
  Serial.print(buf);
#endif
}

// ---------------------------------------------------------------------------
// Handle haptic command from Unity
// Protocol: "FFB A<0-100> B<0-100> C<0-100> D<0-100> E<0-100>"
// Each letter maps to a finger (A=Thumb … E=Pinky).
// Example: "FFB A80 B0 C0 D0 E0" activates thumb haptic at 80% intensity.
// ---------------------------------------------------------------------------
void handleCommand(const String& cmd) {
  if (!cmd.startsWith("FFB")) return;

  const char fingers[] = {'A', 'B', 'C', 'D', 'E'};
  for (int i = 0; i < NUM_FINGERS; i++) {
    int idx = cmd.indexOf(fingers[i]);
    if (idx != -1) {
      int val = cmd.substring(idx + 1).toInt();
      val = constrain(val, 0, 100);
      hapticIntensity[i] = val / 100.0f;
      hapticActive[i] = (val > 0);
    }
  }
}
