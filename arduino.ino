#include <Wire.h>
#include <Adafruit_NeoPixel.h>

// ---------------------------
// AS5600 (steering encoder)
// ---------------------------
#define AS5600_ADDR    0x36
#define RAW_ANGLE_HIGH 0x0C
#define RAW_ANGLE_LOW  0x0D

// ---------------------------
// Analog inputs (throttle / brake)
// ---------------------------
const int throttlePin = A0;
const int brakePin    = A1;

const int analogMin = 190;  // calibration for 0%
const int analogMax = 800;  // calibration for 100%

// ---------------------------
// LED strips (top + bottom)
// ---------------------------
#define TOP_PIN      6
#define BOTTOM_PIN   5

#define TOP_START    7
#define TOP_END      65
#define TOP_COUNT    (TOP_END - TOP_START + 1)

#define BOTTOM_START 5
#define BOTTOM_END   60
#define BOTTOM_COUNT (BOTTOM_END - BOTTOM_START + 1)

#define BRIGHTNESS   150

Adafruit_NeoPixel stripTop(TOP_END + 1, TOP_PIN, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel stripBottom(BOTTOM_END + 1, BOTTOM_PIN, NEO_GRB + NEO_KHZ800);

// Serial input buffer (Unity → Arduino)
String incomingLine;

void setup() {
  Wire.begin();
  Serial.begin(9600);  // Ardity must use 9600

  stripTop.begin();
  stripBottom.begin();
  stripTop.setBrightness(BRIGHTNESS);
  stripBottom.setBrightness(BRIGHTNESS);
  stripTop.clear();
  stripBottom.clear();
  stripTop.show();
  stripBottom.show();

  startupPattern();
}

void loop() {
  // 1) Handle LED commands from Unity
  readSerialCommand();

  // 2) Send steering/throttle/brake to Unity
  sendHandlebarDataToUnity();

  delay(100); // ~10 Hz sensor update
}

// ===========================
// Unity ← Arduino: Send inputs
// ===========================
void sendHandlebarDataToUnity() {
  uint16_t angleRaw = readAS5600Raw();
  float angleDeg = (angleRaw * 360.0) / 4096.0;

  float throttle = normalize(analogRead(throttlePin));
  float brake    = normalize(analogRead(brakePin));

  // The ONLY thing we print to Serial: numeric CSV
  Serial.print((int)angleDeg);
  Serial.print(",");
  Serial.print((int)(throttle * 100));
  Serial.print(",");
  Serial.println((int)(brake * 100));
}

// ===========================
// Unity → Arduino: LED commands
// ===========================
void readSerialCommand() {
  while (Serial.available() > 0) {
    char c = Serial.read();

    if (c == '\n' || c == '\r') {
      if (incomingLine.length() > 0) {
        processCommand(incomingLine);
        incomingLine = "";
      }
    } else {
      incomingLine += c;
    }
  }
}

// Expecting: "LED,<angleDeg>"
void processCommand(const String& cmd) {
  // Turn everything off
  if (cmd == "LED_OFF") {
    for (int i = TOP_START; i <= TOP_END; i++) {
      stripTop.setPixelColor(i, stripTop.Color(0, 0, 0));
    }
    for (int i = BOTTOM_START; i <= BOTTOM_END; i++) {
      stripBottom.setPixelColor(i, stripBottom.Color(0, 0, 0));
    }
    stripTop.show();
    stripBottom.show();
    return;
  }

  // Normal LED angle command: "LED,<angleDeg>"
  if (!cmd.startsWith("LED,")) return;

  int commaIndex = cmd.indexOf(',');
  if (commaIndex < 0) return;

  String angleStr = cmd.substring(commaIndex + 1);
  float angleDeg = angleStr.toFloat();

  showWarningAtAngle(angleDeg);
}


// Map angle (-90..+90) to calibrated LEDs on both strips
void showWarningAtAngle(float angleDeg) {
  // -90° = far left, +90° = far right
  float clamped = constrain(angleDeg, -90.0, 90.0);

  float normalized = (clamped + 90.0) / 180.0;   // 0..1
  normalized = 1.0 - normalized;                // 🔁 flip direction

  // Top strip: [TOP_START .. TOP_END]
  int topOffset = (int)round(normalized * (TOP_COUNT - 1));
  int topIndex  = TOP_START + topOffset;
  topIndex = constrain(topIndex, TOP_START, TOP_END);

  // Bottom strip: [BOTTOM_START .. BOTTOM_END]
  int bottomOffset = (int)round(normalized * (BOTTOM_COUNT - 1));
  int bottomIndex  = BOTTOM_START + bottomOffset;
  bottomIndex = constrain(bottomIndex, BOTTOM_START, BOTTOM_END);

  // Turn everything off first
  for (int i = TOP_START; i <= TOP_END; i++) {
    stripTop.setPixelColor(i, stripTop.Color(0, 0, 0));
  }
  for (int i = BOTTOM_START; i <= BOTTOM_END; i++) {
    stripBottom.setPixelColor(i, stripBottom.Color(0, 0, 0));
  }

  // Light only the selected direction LED on both strips
  stripTop.setPixelColor(topIndex, stripTop.Color(255, 0, 0));
  stripBottom.setPixelColor(bottomIndex, stripBottom.Color(255, 0, 0));

  stripTop.show();
  stripBottom.show();
}


// ===========================
// Startup pattern (optional)
// ===========================
void startupPattern() {
  stripTop.clear();
  stripBottom.clear();

  stripTop.setPixelColor(TOP_START, stripTop.Color(0, 255, 0));
  stripTop.setPixelColor(TOP_START + TOP_COUNT / 2, stripTop.Color(255, 0, 0));
  stripTop.setPixelColor(TOP_END, stripTop.Color(0, 0, 255));

  stripBottom.setPixelColor(BOTTOM_START, stripBottom.Color(0, 255, 0));
  stripBottom.setPixelColor(BOTTOM_START + BOTTOM_COUNT / 2, stripBottom.Color(255, 0, 0));
  stripBottom.setPixelColor(BOTTOM_END, stripBottom.Color(0, 0, 255));

  stripTop.show();
  stripBottom.show();
  delay(1000);

  stripTop.clear();
  stripBottom.clear();
  stripTop.show();
  stripBottom.show();
}

// ===========================
// AS5600 raw angle read
// ===========================
uint16_t readAS5600Raw() {
  Wire.beginTransmission(AS5600_ADDR);
  Wire.write(RAW_ANGLE_HIGH);
  Wire.endTransmission();

  Wire.requestFrom(AS5600_ADDR, 2);
  uint8_t high = Wire.read();
  uint8_t low  = Wire.read();

  return ((high << 8) | low) & 0x0FFF;
}

// ===========================
// Normalize analog to 0..1
// ===========================
float normalize(int rawValue) {
  rawValue = constrain(rawValue, analogMin, analogMax);
  return (rawValue - analogMin) * 1.0 / (analogMax - analogMin);
}
