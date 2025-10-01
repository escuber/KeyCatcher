// #include <Arduino.h>
// #include <Adafruit_NeoPixel.h>
// #include <Preferences.h>
//
// const int BTN_BOOT = 0;        // Boot button (GPIO0)
// const int LED_PIN  = 21;       // NeoPixel
// const int NUMPIX   = 1;
//
// Adafruit_NeoPixel pixels(NUMPIX, LED_PIN, NEO_GRB + NEO_KHZ800);
// Preferences preferences;
//
///* -------------------- timing knobs -------------------- */
// const uint32_t BOUNCE_MS         = 60;    // HIGH must be stable this long to be a real release
// const uint32_t MENU_HOLD_MS      = 5000;  // 5s hold → enter Menu (Yellow pulse)
// const uint32_t FACTORY_EXTRA_MS  = 3000;  // additional hold → arm Factory (Red steady)
// const uint32_t DOUBLE_TAP_MAX_MS = 900;   // max gap between taps
// const uint32_t ARM_WINDOW_MS     = 8000;  // time to complete double-tap once Red
//
///* -------------------- state & LED --------------------- */
// enum class RstState { Idle, Menu, FactoryArm };
// enum class Led      { Off, YellowPulse, RedSteady, BlueBlink, RedBlueBlink };
//
// volatile Led gLed = Led::Off;
// RstState rstState = RstState::Idle;
//
///* -------------------- button debounce ----------------- */
// static bool     lastLow    = true;   // LOW = pressed
// static uint32_t edgeMs     = 0;      // time of last edge
// static uint32_t hiStartMs  = 0;      // when input first went HIGH (begin release)
//
// static bool readLowRaw() { return digitalRead(BTN_BOOT) == LOW; }
//
///* -------------------- tap/hold tracking ---------------- */
// static uint32_t tHold      = 0;
// static uint32_t tLastTap   = 0;
// static uint8_t  tapCount   = 0;
//
// static bool awaitingLift   = false;  // swallow the FIRST lift when entering a state
// static bool releaseUsed    = false;  // one-shot: consume each qualified release once
//
///* -------------------- tiny LED helpers ---------------- */
// static void ledOff()                    { pixels.setPixelColor(0, 0); pixels.show(); }
// static void ledYellowPulse(uint32_t ms) { pixels.setPixelColor(0, ((ms/500)&1)? pixels.Color(40,40,0):0); pixels.show(); }
// static void ledRedSteady(uint32_t ms, bool warn=false){
//   if (warn && ((ms/250)&1)) pixels.setPixelColor(0, 0);        // blink off in last second
//   else                      pixels.setPixelColor(0, pixels.Color(40,0,0));
//   pixels.show();
// }
//
///* -------------------- actions ------------------------- */
// static void SoftResetKeepWifi() {
//   preferences.begin("kc", false);
//   preferences.remove("in_src");
//   preferences.putString("out_src", "USBHID");
//   preferences.putBool("ap_mode", false);
//   preferences.remove("blink_flag");
//   preferences.end();
// }
//
// static void FactoryResetEraseAll() {
//   preferences.begin("kc", false);
//   preferences.clear();
//   preferences.end();
// }
//
///* -------------------- LED painter --------------------- */
// static Led lastLed = Led::Off;
// static uint32_t lastPaintMs = 0;
// void PaintLed()
//{
//   uint32_t now = millis();
//   if (lastLed != gLed) { lastLed = gLed; lastPaintMs = now; } // reset animation phase
//
//   switch (gLed) {
//     case Led::Off:          ledOff(); break;
//     case Led::YellowPulse:  ledYellowPulse(now); break;
//     case Led::RedSteady: {
//       // Flash during the last second of ARM window to warn user
//       bool warn = (rstState == RstState::FactoryArm) && ((now - tLastTap) > (ARM_WINDOW_MS - 1000));
//       ledRedSteady(now, warn);
//       break;
//     }
//     case Led::BlueBlink:    pixels.setPixelColor(0, ((now/500)&1)? pixels.Color(0,0,40):0); pixels.show(); break;
//     case Led::RedBlueBlink: pixels.setPixelColor(0, ((now/500)&1)? pixels.Color(40,0,0):pixels.Color(0,0,40)); pixels.show(); break;
//   }
// }
//
///* -------------------- setup --------------------------- */
// void setup() {
//   Serial.begin(115200);
//   pinMode(BTN_BOOT, INPUT_PULLUP);
//   pixels.begin();
//   pixels.setBrightness(50);
//   ledOff();
//   Serial.println(F("[RST] started"));
// }
//
///* -------------------- main loop ----------------------- */
// void loop()
//{
//   uint32_t now  = millis();
//   bool lowRaw   = readLowRaw();           // LOW = pressed
//
//   /* debounce edge */
//   if (lowRaw != lastLow && (now - edgeMs) >= BOUNCE_MS) {
//     edgeMs = now;
//     lastLow = lowRaw;
//     if (!lowRaw) hiStartMs = now;        // LOW→HIGH (released)
//   }
//
//   switch (rstState)
//   {
//     /* ================== IDLE ================== */
//     case RstState::Idle:
//       // track hold duration
//       if (lowRaw) tHold = (tHold == 0) ? now : tHold; else tHold = 0;
//
//       if (lowRaw && (now - tHold) >= MENU_HOLD_MS) {
//         rstState     = RstState::Menu;
//         gLed         = Led::YellowPulse;
//         tapCount     = 0;
//         awaitingLift = true;             // swallow the very first lift
//         releaseUsed  = false;
//         Serial.println(F("[RST] → MENU (yellow)"));
//       }
//       break;
//
//     /* ================== MENU (YELLOW) ================== */
//     case RstState::Menu:
//       // count *qualified* releases once each
//       if (!lowRaw && !releaseUsed && (now - hiStartMs) >= BOUNCE_MS) {
//         if (awaitingLift) {
//           awaitingLift = false;          // ignore first lift
//           // do not touch tapCount
//         } else {
//           tapCount = (now - tLastTap <= DOUBLE_TAP_MAX_MS) ? (tapCount + 1) : 1;
//           tLastTap = now;
//           Serial.printf("[RST] menu tap=%u\n", tapCount);
//         }
//         releaseUsed = true;              // consume this HIGH
//       }
//       if (lowRaw) releaseUsed = false;   // allow next release
//
//       if (tapCount >= 2) {
//         Serial.println(F("[RST] SOFT reset (keep Wi-Fi)"));
//         SoftResetKeepWifi();
//         gLed = Led::BlueBlink;
//         delay(400);                      // small visual confirm
//         ESP.restart();
//       }
//
//       // keep holding after Yellow to arm Factory (go to Red)
//       if (lowRaw && (now - tHold) >= (MENU_HOLD_MS + FACTORY_EXTRA_MS)) {
//         rstState     = RstState::FactoryArm;
//         gLed         = Led::RedSteady;
//         awaitingLift = true;             // must lift once before taps count
//         releaseUsed  = false;
//         tapCount     = 0;
//         tLastTap     = now;              // start the ARM window clock
//         Serial.println(F("[RST] → FACTORY ARM (red)"));
//       }
//       break;
//
//     /* ================== FACTORY ARM (RED) ================== */
//     case RstState::FactoryArm:
//       // first lift after entering Red is ignored
//       if (!lowRaw && !releaseUsed && (now - hiStartMs) >= BOUNCE_MS) {
//         if (awaitingLift) {
//           awaitingLift = false;          // swallow the first lift (don’t count)
//           // don't set tapCount or tLastTap here
//         } else {
//           tapCount = (now - tLastTap <= DOUBLE_TAP_MAX_MS) ? (tapCount + 1) : 1;
//           tLastTap = now;
//           Serial.printf("[RST] factory tap=%u\n", tapCount);
//         }
//         releaseUsed = true;
//       }
//       if (lowRaw) releaseUsed = false;   // allow next release
//
//       if (tapCount >= 2) {
//         Serial.println(F("[RST] FACTORY RESET (erase all)"));
//         FactoryResetEraseAll();
//         gLed = Led::RedBlueBlink;
//         delay(600);
//         ESP.restart();
//       }
//
//       // Timeout if user stops interacting in Red
//       if (!lowRaw && (now - tLastTap) > ARM_WINDOW_MS) {
//         Serial.println(F("[RST] factory arm timeout → idle"));
//         rstState = RstState::Idle;
//         gLed     = Led::Off;
//         tHold    = 0;
//         tapCount = 0;
//       }
//       break;
//   }
//
//   PaintLed();
// }

#include <Arduino.h>
#include <Adafruit_NeoPixel.h>
#include <Preferences.h>
#include <BleKeyboard.h>
#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include <BLE2902.h>
#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>


#include <WiFi.h>
#include <WiFiUdp.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "ArduinoJson.h"
#include "mbedtls/base64.h" // comes with ESP32 Arduino core
#include <vector>
#include <algorithm>
#include "Reset.h"

#include <deque>

const int BTN_BOOT = 0; // GPIO0
const int LED_PIN = 21; // your NeoPixel pin
Adafruit_NeoPixel pixels(1, LED_PIN, NEO_GRB + NEO_KHZ800);

// Your persistent storage helpers
#include <Preferences.h>
Preferences preferences;

// your existing things…
extern WiFiUDP Udp;
//extern const uint16_t localUdpPort;

struct WifiCred { String ssid; String pass; };
//void LoadCredList(std::vector<WifiCred>& out);
void saveconfig();



// status flags you already use
volatile bool gApUp = false;
volatile bool gStaConnected = false;

// task handle so we can see/stop the task if needed
static TaskHandle_t s_staTask = nullptr;




static String apNameFromMac(const char* base = "KeyCatcher") {
  uint8_t mac[6]; esp_read_mac(mac, ESP_MAC_WIFI_SOFTAP);
  char buf[32];
  snprintf(buf, sizeof(buf), "%s-%02X%02X", base, mac[4], mac[5]);
  return String(buf);
}

static void startSoftAP(const String& ssid) {
  WiFi.mode(WIFI_AP_STA);               // allows STA trials later
  bool ok = WiFi.softAP(ssid.c_str());  // (open AP) add password if desired
  IPAddress ip = WiFi.softAPIP();
  Serial.printf("Soft-AP %s: %s, IP=%s\n",
                ssid.c_str(), ok ? "ENABLED" : "FAILED",
                ip.toString().c_str());
  gApUp = ok;
}

void SoftResetKeepWifi()
{
  preferences.begin("kc", false);
  preferences.remove("in_src");
  preferences.putString("out_src", "USBHID");
  preferences.putBool("ap_mode", false);
  preferences.remove("blink_flag");
  preferences.end();
}

void FactoryResetEraseAll()
{
  preferences.begin("kc", false);
  preferences.clear();
  preferences.end();
}
static inline String TrimPingCmd(String s)
{
  s.trim();
  if (s.endsWith("<<END>>"))
    s.remove(s.length() - 7);
  s.trim();
  return s;
}
ResetMenu menu(BTN_BOOT, pixels);

static std::deque<std::string> gBleRxQ;
class BLEServerCallbacks; // from the BLE headers
BLEServerCallbacks *MakeKCServerCallbacks();
// ==== KC protocol knobs ====
static const uint16_t KC_JSON_CAP = 384;         // enough for small envelopes
static const uint32_t KC_CHUNK_TIMEOUTMS = 5000; // drop half messages after this idle
static const uint16_t KC_MAX_ACCUM = 6143; //4096;       // max assembled msg bytes, tune as needed
enum class KCTransport
{
  Udp,
  Ble
};
static const uint16_t KC_BEFORE_FIRST_MS = 120; // settle time before first char of each message
static const bool KC_WAKEUP_SPACE = false;      // set true if you still see a clipped first char

// extern "C"
// {
// #include "tusb.h"
// }
const int buttonPin = 5;
#define PIN 21
#define NUMPIXELS 1
#define DELAYVAL 500
#define SETUP_PIN 1
#define CHARACTERISTIC_UUID "0000bbbb-0000-1000-8000-00805f9b34fb"
#define TX_CHARACTERISTIC_UUID "0000bbbc-0000-1000-8000-00805f9b34fb"
static const char *SERVICE_UUID = "0000aaaa-0000-1000-8000-00805f9b34fb";
static const char *RX_CHARACTERISTIC = "0000bbbb-0000-1000-8000-00805f9b34fb"; // write from app
static const char *TX_CHARACTERISTIC = "0000bbbc-0000-1000-8000-00805f9b34fb"; // notify to app
static const uint16_t KC_UDP_PORT = 4210;

//static bool gStaConnected = false;   // true when WiFi.status()==WL_CONNECTED
//static bool gApUp         = false;   // true when AP is running
static bool gBleEnabled   = false;   // true when BLE input is enabled (BOTH or BLE)

// Tunables for LED look
static const uint8_t LED_R = 40;     // red channel intensity
static const uint8_t LED_B = 40;     // blue channel intensity


static const uint32_t BLINK_WINDOW_MS = 30000; // 30s max "find me"





static const uint32_t BLINK_MS = 800;          // blink period
//static const uint32_t BLINK_WINDOW_MS = 30000; // "find me" timeout

static bool gBlinkNeeded = true;
static uint32_t gBlinkDeadlineMs = 0;

//static inline void ArmBlinkWindow()    { gBlinkNeeded = true;  gBlinkDeadlineMs = millis() + BLINK_WINDOW_MS; }
//static inline void MarkReachable()     { gBlinkNeeded = false; }
static inline bool InBlinkWindow()     { return gBlinkNeeded && (int32_t)(gBlinkDeadlineMs - millis()) > 0; }

static void LedOff() { pixels.setPixelColor(0, 0); pixels.show(); }
static void SetLed(uint8_t r, uint8_t g, uint8_t b) { pixels.setPixelColor(0, pixels.Color(g, r, b)); pixels.show(); }





BLECharacteristic *gTxChar = nullptr;      // notify
const uint16_t DISCOVER_REPLY_PORT = 4211; // WinUI listens here after broadcast
// Adafruit_NeoPixel pixels(NUMPIXELS, PIN, NEO_GRB + NEO_KHZ800);
BLEServer *gServer = nullptr;
bool bleConnected = false;
unsigned long lastBleActivity = 0;
const unsigned long BLE_INACTIVITY_TIMEOUT = 60000; // 10 seconds (tweak as needed)
USBHIDKeyboard Keyboard;
BleKeyboard bleKeyboard("KeyCatcher", "KeyCatcher Inc.", 100);
BLECharacteristic *pTxCharacteristic = nullptr;
static uint16_t gConnId = 0;
WiFiUDP Udp;
// Preferences preferences;
//unsigned int localUdpPort = 4210;

//static bool gBlinkNeeded = true;       // start true on boot
//static uint32_t gBlinkDeadlineMs = 0;  // stop blinking after window
///static const uint32_t BLINK_WINDOW_MS = 30000; // 30s "find me" max

static inline void MarkReachable()
{
  gBlinkNeeded = false;                 // stop blinking after we know the app found us
}

static inline void ArmBlinkWindow()
{
  gBlinkNeeded = true;
  gBlinkDeadlineMs = millis() + BLINK_WINDOW_MS;
}










struct KC_Config
{
  String ssid = "";
  String password = "";
  String input_source = "WIFI";    // "BLE", "WIFI", or "BOTH"
  String output_source = "USBHID"; // "USBHID" or "BLEHID"
  bool ap_mode = true;
  String blink_flag = "";
  String creds = "[]";
} config;

inline bool KC_IsBLE() { return config.output_source == "BLEHID" && bleKeyboard.isConnected(); }
inline void KC_WriteChar(char c)
{
  if (KC_IsBLE())
    bleKeyboard.write(c);
  else
    Keyboard.write(c);
}
inline void KC_Enter()
{
  if (KC_IsBLE())
    bleKeyboard.write(KEY_RETURN);
  else
    Keyboard.write(KEY_RETURN);
}
inline void KC_Tab()
{
  if (KC_IsBLE())
    bleKeyboard.write(KEY_TAB);
  else
    Keyboard.write(KEY_TAB);
}

#if __has_include("tusb.h")
extern "C"
{
#include "tusb.h"
}
// If device stack isn’t enabled, the symbols still won’t exist,
// so also check the config macro.
#ifndef CFG_TUD_ENABLED
#define CFG_TUD_ENABLED 0
#endif
#else
#define CFG_TUD_ENABLED 0
#endif

// BLE
// BLEServer*        gServer  = nullptr;
BLEService *gSvc = nullptr;
BLECharacteristic *gRxChar = nullptr; // write
// BLECharacteristic* gTxChar = nullptr;  // notify
static void StripDiscoveryTokens(String &s)
{
  s.replace("KC:DISCOVER?", "");
  s.replace("KC:DISCOVERY", "");
  s.replace("DISCOVER_KEYCATCHER", "");
  s.trim(); // remove leading/trailing blanks
}

static inline bool IsBleInputEnabled()
{
  // “Putting out bluetooth” = input side enabled (advertising/mailbox).
  // We consider BLE enabled when input_source is BLE or BOTH.
  return config.input_source == "BLE" || config.input_source == "BOTH";
}

// static inline void LedOff()
// {
//   pixels.setPixelColor(0, 0);
//   pixels.show();
// }




// static inline bool InBlinkWindow() {
//   return gBlinkNeeded && (int32_t)(gBlinkDeadlineMs - millis()) > 0;
// }

// Simple helpers; use your existing LED color function
//static void LedOff() { pixels.setPixelColor(0, 0); pixels.show(); }

// static void SetLed(uint8_t r, uint8_t g, uint8_t b) {
//   pixels.setPixelColor(0, pixels.Color(r, g, b));
//   pixels.show();
// }

// apOnly = true when running only Soft-AP (no STA link)
// staUp  = true when STA is connected to a network
// bleOn  = true when BLE input is enabled/advertising (or connected)
// Colors per your plan: purple = BLE+STA, red = STA w/o BLE, blue = AP (w/ BLE) else red for AP w/o BLE
void UpdateStatusLed()
{    if (menu.led() != ResetMenu::Led::Off) return;
  bool staUp = (WiFi.status() == WL_CONNECTED);
  bool apUp  = (WiFi.getMode() & WIFI_MODE_AP) != 0;
  bool apOnly = apUp && !staUp; // AP up, STA not connected
  bool bleOn = (config.input_source == "BLE" || config.input_source == "BOTH");

  uint8_t r=0,g=0,b=0;

  if (staUp) {
    if (bleOn) { r = 40; g = 0;  b = 40; } // purple
    else       { r = 40; g = 0;  b = 0;  } // red
  } else { // AP-only
    if (bleOn) { r = 0;  g = 0;  b = 40; } // blue
    else       { r = 40; g = 0;  b = 0;  } // red
  }

  // blink only in AP-only "find me" window
  if (apOnly && InBlinkWindow()) {
    bool on = ((millis() / (BLINK_MS / 2)) % 2) == 0;
    if (!on) { LedOff(); return; }
  } else {
    gBlinkNeeded = false; // once contacted or timeout → steady
  }

  SetLed(r,g,b);
}

// Call this often; it handles blink timing & menu override.
void xUpdateStatusLed()
{
  // Don't fight the reset menu animation
  if (menu.led() != ResetMenu::Led::Off) return;

  // Determine base color: red if connected, red if AP-only (blink later)
  bool apOnly = (gApUp && !gStaConnected);

  // Base channels
  uint8_t r = LED_R;
  uint8_t g = 0;
  uint8_t b = 0;

  // Add blue “portion” iff BLE is enabled in config
  if (gBleEnabled) b = LED_B;

  // Blink when AP-only; solid when STA connected
  if (apOnly&& (gBlinkNeeded && (int32_t)(millis() - gBlinkDeadlineMs) > 0))
  {
    // 50% duty
    bool on = (millis() / (BLINK_MS / 2)) % 2 == 0;
    if (!on)
    {
      LedOff();
      return;
    }
  }
  //Serial.printf("LED R=%u G=%u B=%u\n", r, g, b);

  pixels.setPixelColor(0, pixels.Color(g, r, b));
  pixels.show();
}


struct KCInflight
{
  uint32_t id = 0;
  uint16_t total = 0; // expected chunk count, field "n"
  uint16_t next = 0;  // next expected index "i"
  std::string buf;    // assembled raw bytes
  bool fromUdp = false;
  IPAddress udpIp;
  uint16_t udpPort = 0;
  unsigned long lastMs = 0;

  inline void KC_WriteUSB(char c) { Keyboard.write(c); }
  inline void KC_WriteBLE(char c) { bleKeyboard.write(c); }
  inline void KC_EnterUSB() { Keyboard.write(KEY_RETURN); }
  inline void KC_TabUSB() { Keyboard.write(KEY_TAB); }
  inline void KC_EnterBLE() { bleKeyboard.write(KEY_RETURN); }
  inline void KC_TabBLE() { bleKeyboard.write(KEY_TAB); }

  // static bool prevLow = true;    // LOW = pressed
  // static uint32_t edgeMs = 0;    // millis() at last edge
  // static uint32_t hiStartMs = 0; // millis() when pin first went HIGH

  void reset()
  {
    id = 0;
    total = 0;
    next = 0;
    buf.clear();
    fromUdp = false;
    udpIp = IPAddress();
    udpPort = 0;
    lastMs = 0;
  }
} kc;
// const int BTN_BOOT = 0; // BOOT button (GPIO0)
// const int LED_PIN = 21; // NeoPixel or a plain RGB
bool awaitingRelease = false;
// Timing
const uint32_t MENU_HOLD_MS = 5000;     // enter menu
const uint32_t FACTORY_EXTRA_MS = 3000; // extra hold to arm red
const uint32_t DOUBLE_TAP_MAX_MS = 600;
const uint32_t ARM_WINDOW_MS = 3000; // time to double-tap after release
static void kcSendAckUdp(const IPAddress &ip, uint16_t port, uint32_t id, uint16_t idx);
static void kcSendAckBle(uint32_t id, uint16_t idx);
static void kcSendFinalUdp(const IPAddress &ip, uint16_t port, const uint8_t *p, size_t n);
static void kcSendFinalBle(const uint8_t *p, size_t n);

static const uint16_t KC_TYPE_MS = 14;        // per printable char
static const uint16_t KC_AFTER_ENTER_MS = 80; // after Enter/Tab
static const uint8_t KC_BURST = 10;           // chars per burst
static const uint16_t KC_BURST_GAP_MS = 35;   // gap between bursts

enum class RstState
{
  Idle,
  Menu,
  FactoryArm
};
RstState rstState = RstState::Idle;

uint32_t tHold = 0;    // when button became LOW
uint32_t tLastTap = 0; // last rising edge
uint8_t tapCount = 0;  // within current tap window

String rxBuffer = "";
bool inSetup = false;
void USBtypeEnter()
{
  Keyboard.press(KEY_RETURN);
  delay(5);
  Keyboard.releaseAll();
}
void BLEtypeEnter()
{
  bleKeyboard.press(KEY_RETURN);
  delay(5);
  bleKeyboard.releaseAll();
}
void BLEtypeShiftEnter()
{
  bleKeyboard.press(KEY_LEFT_SHIFT);
  bleKeyboard.press(KEY_RETURN); // or KEY_ENTER depending on lib
  delay(5);
  bleKeyboard.releaseAll();
}
static volatile bool gTypingBusy = false;

static uint32_t Hash32(const String& s) {
  // very small CRC32-ish; replace with your CRC if you have one
  uint32_t h = 2166136261u;
  for (size_t i = 0; i < s.length(); ++i) { h ^= (uint8_t)s[i]; h *= 16777619u; }
  return h ? h : 1;
}



struct RecentMsg { uint32_t hash; uint32_t seenAt; };
static RecentMsg gRecent[4]; // tiny LRU
static bool SeenRecently(uint32_t h, uint32_t now, uint32_t windowMs = 15000) {
  for (auto& r : gRecent) if (r.hash == h && (now - r.seenAt) < windowMs) return true;
  // insert/rotate
  for (auto& r : gRecent) if (r.hash == 0) { r.hash = h; r.seenAt = now; return false; }
  gRecent[0] = {h, now};
  return false;
}

void blinkBlue(int times)
{ // cyan
  for (int i = 0; i < times; ++i)
  {
    pixels.setPixelColor(0, pixels.Color(0, 0, 40));
    pixels.show();
    delay(150);
    pixels.setPixelColor(0, 0);
    pixels.show();
    delay(150);
  }
}
void showYellowPulse(int times)
{ //' really orange'
  for (int i = 0; i < times; ++i)
  {
    pixels.setPixelColor(0, pixels.Color(40, 20, 0));
    pixels.show();
    delay(150);
    pixels.setPixelColor(0, 0);
    pixels.show();
    delay(150);
  }
}
void showRed(int times)
{ //
  for (int i = 0; i < times; ++i)
  {
    pixels.setPixelColor(0, pixels.Color(40, 0, 0));
    pixels.show();
    delay(150);
    pixels.setPixelColor(0, 0);
    pixels.show();
    delay(150);
  }
}

// blinkRedBlue

void showRedSteady(int times)
{ //
  for (int i = 0; i < times; ++i)
  {
    pixels.setPixelColor(0, pixels.Color(40, 0, 0));
    pixels.show();
    delay(150);
    pixels.setPixelColor(0, 0);
    pixels.show();
    delay(150);
  }
}

void blinkRedBlue(int times)
{ //
  for (int i = 0; i < times; ++i)
  {
    pixels.setPixelColor(0, pixels.Color(0, 40, 0));
    pixels.show();
    delay(150);
    pixels.setPixelColor(0, 0);
    pixels.show();
    delay(150);
    pixels.setPixelColor(0, pixels.Color(0, 40, 40));
    pixels.show();
    delay(150);
    pixels.setPixelColor(0, 0);
    pixels.show();
    delay(150);
  }
}

void LEDoff(int times)
{ //
  for (int i = 0; i < times; ++i)
  {
    pixels.setPixelColor(0, 0);
    pixels.show();
    delay(150);
  }
}

String getConfig()
{

  return String("{") +
         "\"ssid\":\"" + config.ssid + "\"," +
         "\"pss\":\"" + config.password + "\"," +
         "\"in\":\"" + config.input_source + "\"," +
         "\"out\":\"" + config.output_source + "\"," +
         "\"ap\":" + (config.ap_mode ? "true" : "false") + "," +
         "\"creds\":" + config.creds + "," +
         "\"bflag\":\"" + config.blink_flag + "\"" +
         "}";
}

void bleNotifyConfigChunked(const String &json)
{
  Serial.println("msg:"+json);
  if (!gTxChar)
    return;
  constexpr size_t PAYLOAD = 16;
  uint8_t buf[PAYLOAD + 4];

  gTxChar->setValue((uint8_t *)"CONFIG_START:", 13);
  gTxChar->notify();
  delay(8);
  for (size_t i = 0; i < json.length(); i += PAYLOAD)
  {
    size_t n = std::min(PAYLOAD, json.length() - i);
    memcpy(buf, "C_P:", 4);
    memcpy(buf + 4, json.c_str() + i, n);
    gTxChar->setValue(buf, 4 + n);
    gTxChar->notify();
    Serial.printf("Sent chunk: %d-%d\n", i, i+n);
    delay(8);
  }
  gTxChar->setValue((uint8_t *)"CONFIG_END", 10);
  gTxChar->notify();
}
String SanitizeForHid(String s)
{
  // common punctuation
  s.replace("“", "\"");
  s.replace("”", "\"");
  s.replace("‘", "'");
  s.replace("’", "'");
  s.replace("–", "-");
  s.replace("—", "-");   // en/em dash
  s.replace("…", "..."); // ellipsis
  s.replace("•", "-");
  s.replace("·", "-");
  s.replace("→", "->");
  s.replace("⇒", "=>");
  s.replace("✓", "[x]");
  s.replace("\xC2\xA0", " "); // non-breaking space

  // strip any remaining non-ASCII bytes
  String out;
  out.reserve(s.length());
  for (size_t i = 0; i < s.length(); ++i)
  {
    unsigned char c = static_cast<unsigned char>(s[i]);
    if (c == '\r' || c == '\n' || c == '\t')
    {
      out += char(c);
      continue;
    }
    if (c >= 0x20 && c <= 0x7E)
    {
      out += char(c);
      continue;
    }
    // else drop it
  }
  return out;
}

static void dumpHex(const uint8_t *p, size_t n)
{
  return;
  for (size_t i = 0; i < n; ++i)
  {
    if (i && (i % 16) == 0)
      Serial.println();
    Serial.printf("%02X ", p[i]);
  }
  Serial.println();
}
static void dumpAscii(const uint8_t *p, size_t n)
{
return ;
  for (size_t i = 0; i < n; ++i)
  {
    char c = (char)p[i];
    Serial.print((c >= 32 && c < 127) ? c : '.');
  }
  Serial.println();
}
void TypeTextPaced(const String &msg)
{
  // 1) Sanitize to ASCII (keep your SanitizeForHid)
  String safe = SanitizeForHid(msg);

  // 2) Ensure no modifier is “stuck”
  Keyboard.releaseAll();         // USB
  if (bleKeyboard.isConnected()) // BLE
    bleKeyboard.releaseAll();

  // 3) Give the target a moment to settle before FIRST char
  delay(KC_BEFORE_FIRST_MS);

  // 3a) Optional wake-up: type a space then backspace (some apps need an initial “poke”)
  if (KC_WAKEUP_SPACE)
  {
    if (KC_IsBLE())
      bleKeyboard.write(' ');
    else
      Keyboard.write(' ');
    delay(30);
    if (KC_IsBLE())
      bleKeyboard.write(KEY_BACKSPACE);
    else
      Keyboard.write(KEY_BACKSPACE);
    delay(30);
  }

  // 4) Your paced typewriter loop
  uint8_t burst = 0;
  // for (char c : safe)
  size_t i = 0;
  const size_t n = safe.length();

  while (i < n)
  {

    char c = safe.charAt(i);
    // 1) Visible symbol ?
    // Note: ? is multi-byte in UTF-8, so compare as a substring
    if (c == '<')
    {
      // You can normalize case or check multiple variants
      if (i + 7 <= n && safe.substring(i, i + 7).equalsIgnoreCase("<enter>"))
      {
        KC_Enter();
        delay(KC_AFTER_ENTER_MS);
        burst = 0;
        i += 7;
        continue;
      }
      if (i + 13 <= n && safe.substring(i, i + 13).equalsIgnoreCase("<shift-enter>"))
      {
        BLEtypeShiftEnter();
        delay(KC_AFTER_ENTER_MS);
        i += 13;
        continue;
      }
    }

    if (c == '\r' || c == '\n')
    {
      KC_Enter();
      delay(KC_AFTER_ENTER_MS);
      burst = 0;
      i++;
      continue;
    }
    if (c == '\t')
    {
      KC_Tab();
      delay(KC_AFTER_ENTER_MS);
      burst = 0;
      i++;
      continue;
    }

    KC_WriteChar(c);
    delay(KC_TYPE_MS);

    if (c == '.' || c == ',' || c == ';' || c == ':' || c == '!' || c == '?' || c == ')' || c == ']' || c == '}')
      delay(KC_TYPE_MS + 8);

    if (++burst >= KC_BURST)
    {
      delay(KC_BURST_GAP_MS);
      burst = 0;
    }
    i++;
  }

  // 5) Clean end
  Keyboard.releaseAll();
  if (bleKeyboard.isConnected())
    bleKeyboard.releaseAll();
}
void saveConfig()
{
  preferences.begin("kc", false);
  preferences.putBool("ap_mode", config.ap_mode);
  preferences.putString("ssid", config.ssid);
  preferences.putString("password", config.password);
  preferences.putString("in_src", config.input_source);
  preferences.putString("out_src", config.output_source);
  preferences.putString("blink_flag", config.blink_flag);
  preferences.putString("creds", config.creds);
  preferences.end();
}
bool reboot_flag = false;
// ----- CONFIG MESSAGE PARSER -----
void parseAndSaveConfig(String setupMsg)
{

  Serial.print("Config with:");
  // Serial.println(setupMsg);

  // pixels.setPixelColor(0, pixels.Color(40, 40, 0)); // Yellow
  int uidStart = setupMsg.indexOf("ssid:") + 5;
  int uidEnd = setupMsg.indexOf("\n", uidStart);
  String uid = setupMsg.substring(uidStart, uidEnd);
  Serial.print("ssid:");
  Serial.println(uid);

  int passStart = setupMsg.indexOf("password:") + 9;
  int passEnd = setupMsg.indexOf("\n", passStart);
  String password = setupMsg.substring(passStart, passEnd);

  int credsStart = setupMsg.indexOf("creds:") + 6;
  int credsEnd = setupMsg.indexOf("\n", credsStart);
  String credsstr = setupMsg.substring(credsStart, credsEnd);
  config.creds = credsstr;

  int inSrcStart = setupMsg.indexOf("input_source:") + 13;
  int inSrcEnd = setupMsg.indexOf("\n", inSrcStart);
  String input_source = setupMsg.substring(inSrcStart, inSrcEnd);

  if (input_source != config.input_source)
    reboot_flag = true;

  int outSrcStart = setupMsg.indexOf("output_source:") + 14;
  int outSrcEnd = setupMsg.indexOf("\n", outSrcStart);
  // String output_source = setupMsg.substring(outSrcStart, outSrcEnd);

  String output_source = setupMsg.substring(outSrcStart, outSrcEnd);
  output_source.trim(); // ← same thing on incoming setup
  if (output_source != config.output_source)
    reboot_flag = true;

  config.output_source = output_source;

  int apModeStart = setupMsg.indexOf("ap_mode:") + 8;
  int apModeEnd = setupMsg.indexOf("\n", apModeStart);
  String ap_mode_str = setupMsg.substring(apModeStart, apModeEnd);
  bool ap_mode = (ap_mode_str == "true");
  if (ap_mode != config.ap_mode)
    reboot_flag = true;
  config.ap_mode = ap_mode;

  config.ssid = uid;
  config.password = password;
  config.input_source = input_source;
  config.output_source = output_source;
  Serial.print("[load config] SSId: ");
  Serial.print(config.ssid);

  // Serial.print("[load password] passoword: ");
  Serial.print(" passoword: ");
  Serial.print(config.password);

  Serial.print(" creds: ");
  Serial.print(config.creds);

  Serial.print(" AP Mode: ");
  Serial.print(config.ap_mode ? "Enabled" : "Disabled");
  Serial.print(" InputSource=");
  Serial.print(config.input_source);
  Serial.print(" output=");
  Serial.print(config.output_source);
  Serial.print(" blink flag=");
  Serial.print(config.blink_flag);
  int blinkStart = setupMsg.indexOf("blink:") + 6;
  int blinkEnd = setupMsg.indexOf("\n", blinkStart);
  String blink_flag = "";
  if (blinkStart > 5 && blinkEnd > blinkStart)
  {
    blink_flag = setupMsg.substring(blinkStart, blinkEnd);
    config.blink_flag = blink_flag;
  }
  else
  {
    config.blink_flag = "";
  }

  saveConfig();
  Serial.println("Config updated!");
}
static void kcMaybeTimeout()
{
  if (kc.id && (millis() - kc.lastMs) > KC_CHUNK_TIMEOUTMS)
  {
    kc.reset();
  }
}

static inline String TrimEndToken(String s)
{
  if (s.endsWith("<<END>>"))
    s.remove(s.length() - 7);
  s.trim();
  return s;
}
static inline bool isGetConfigCmd(const String &s)
{
  String t = s;
  t.trim();
  if (t.endsWith("<<END>>"))
    t.remove(t.length() - 7);
  return t == "get_config" || t.startsWith("get_config ");
}
void processIncoming(const String &raw)
{
  IPAddress rip = Udp.remoteIP();
  uint16_t rport = Udp.remotePort();
  String msg = raw;
  //Serial.println("processIncomming:" + msg);
  /* 1. Setup block unchanged … */
  if (raw.startsWith("<setup>"))
  {
    Serial.println("have a setup");
    Serial.println(raw);
    inSetup = true;
    rxBuffer = raw;
    if (raw.endsWith("<endsetup><<END>>"))
    {
      parseAndSaveConfig(rxBuffer);
      rxBuffer = "";
      inSetup = false;
      delay(1000); // Wait for 5 seconds
      if (reboot_flag)
      {
        Serial.println("Restarting");
        ESP.restart();
      }
    }
    else
    {
      Serial.println("Dont have the end");
    }
  }
  else if (inSetup)
  {
    Serial.println("insetup");
    Serial.println("Next message in setup");
    Serial.println(raw);
    rxBuffer += raw;
    if (raw.endsWith("<endsetup><<END>>"))
    {
      parseAndSaveConfig(rxBuffer);
      rxBuffer = "";
      inSetup = false;
      delay(1000); // Wait for 5 seconds
      if (reboot_flag)
      {
        Serial.println("Restarting");
        ESP.restart();
      }
      // Serial.println("Restarting");
      // ESP.restart();
    }
  }
  else
  {

    if (TrimPingCmd(msg).equalsIgnoreCase("ping"))
    {
      MarkReachable();
      Udp.beginPacket(rip, rport);
      Udp.print("pong");
      Udp.endPacket();
      return;
    }
    /* 2. Normal message -------------------------------------------- */
    // String msg = raw;
    //    Serial.println("processIncomming:" + msg);
    if (isGetConfigCmd(msg))
    {
      Serial.println("get_config caught in processIncoming");
      bleNotifyConfigChunked(getConfig()); // send JSON over notify
      return;                              // do not type
    }

    if (msg.endsWith("<<END>>"))
      msg.remove(msg.length() - 7);

    //Serial.print("Processing message: ");
    //Serial.println(msg);

    String clean = msg;
    if (clean.endsWith("<<END>>"))
      clean.remove(clean.length() - 7);

    /* --- USB HID --------------------------------------------------- */
    if (config.output_source == "USBHID")
    {

      String safe = SanitizeForHid(msg);

      Serial.println("Typing (USBHID): " + msg);

      TypeTextPaced(msg);
      uint8_t burst = 0;
    }
    else if (config.output_source == "BLEHID" && bleKeyboard.isConnected())
    {
      Serial.println("Typing (BLEHID): " + msg);

      TypeTextPaced(msg); // this routes to bleKeyboard via KC_IsBLE()

      //     bleKeyboard.print(msg.c_str());
      Serial.println("Typed (BLEHID): " + msg);
    }
  }
}

static std::string kcProcessFullMessage(const uint8_t *data, size_t len)
{
  // Assemble -> hand to normal handler
  String msg((const char *)data, len);
  processIncoming(msg);

  // Short OK reply
  StaticJsonDocument<64> doc;
  doc["t"] = "kc_ok";
  std::string out;
  serializeJson(doc, out);
  return out;
}

static void StartTypingJob(const String& msg) {
  if (gTypingBusy) return;         // already typing, ignore
  gTypingBusy = true;
  processIncoming(msg);            // this does the slow typing
  gTypingBusy = false;
}

static void kcHandleEnvelope(const uint8_t *data, size_t len,
                             KCTransport tr,
                             const IPAddress &ip = IPAddress(), uint16_t port = 0)
{
  kcMaybeTimeout();
//Serial.println("kcHandleEnvelope");

//Serial.printf("Raw [%d]: ", (int)len);
//Serial.write(data, len);
//Serial.println();
  StaticJsonDocument<KC_JSON_CAP> doc;
  if (deserializeJson(doc, data, len))
  {
    Serial.println(".1");
    return;
  }
//Serial.println(".2");
  const char *t = doc["t"] | "";
  if (strcmp(t, "kc_chunk") != 0)
    return;
//Serial.println("2");
  uint32_t id = doc["id"] | 0U;
  uint16_t n = doc["n"] | 0U;
  uint16_t idx = doc["i"] | 0U;
  const char *plB64 = doc["pl"] | "";
  if (!id || !n || idx >= n)
    return;
//Serial.println("3");
  if (kc.id == 0)
  {
    kc.id = id;
    kc.total = n;
    kc.next = 0;
    kc.buf.clear();
    kc.buf.reserve(std::min<uint32_t>(KC_MAX_ACCUM, static_cast<uint32_t>(n) * 220));
    kc.fromUdp = (tr == KCTransport::Udp);
    kc.udpIp = ip;
    kc.udpPort = port;
  }
//Serial.println("4");
  if (id != kc.id || n != kc.total)
    return;
  if (idx != kc.next)
    return;
//Serial.println("5");
  size_t srcLen = strlen(plB64);
  size_t needLen = 0;
  mbedtls_base64_decode(nullptr, 0, &needLen, (const unsigned char *)plB64, srcLen);
  if (needLen == 0)
    return;

  if (kc.buf.size() + needLen > KC_MAX_ACCUM)
  {
    kc.reset();
    return;
  }
//Serial.println("6");
  size_t old = kc.buf.size();
  kc.buf.resize(old + needLen);
  size_t outLen = 0;
  int rc = mbedtls_base64_decode((unsigned char *)&kc.buf[old], needLen, &outLen,
                                 (const unsigned char *)plB64, srcLen);
  if (rc != 0 || outLen != needLen)
  {
    kc.reset();
    return;
  }
//Serial.println("7");
  kc.lastMs = millis();
  kc.next = idx + 1;

  // (optional) visibility while testing
  //Serial.printf("[KC] chunk %u/%u, acc=%u\n", (unsigned)(idx + 1), (unsigned)n, (unsigned)kc.buf.size());

  if (kc.fromUdp)
    kcSendAckUdp(kc.udpIp, kc.udpPort, kc.id, idx);
  else
    kcSendAckBle(kc.id, idx);

  const bool isLast = (kc.next == kc.total);
  if (isLast)
{
  // small guard so the client can process the last ACK cleanly
  delay(20);
String msg((const char*)kc.buf.data(), kc.buf.size());
Serial.println('msg:'+msg);

// Optional special handling
bool handled = false;
if (msg == "get_config" || msg.startsWith("get_config")) {
  bleNotifyConfigChunked(getConfig()); // or UDP path if it arrived via UDP
  handled = true;
}

// Always send final OK immediately so the client completes
StaticJsonDocument<32> ok; ok["t"] = "kc_ok";
char out[32]; size_t m = serializeJson(ok, out, sizeof(out));
if (kc.fromUdp) {
  Udp.beginPacket(kc.udpIp, kc.udpPort);
  Udp.write((const uint8_t*)out, m);
  Udp.endPacket();
} else if (gTxChar) {
  gTxChar->setValue((uint8_t*)out, m);
  gTxChar->notify();
}

// Free state so a new send can begin while we type
kc.reset();

// If get_config already replied, we are done
if (handled) return;

// Dedupe and start the typing job
uint32_t h = Hash32(msg);
uint32_t now = millis();
if (SeenRecently(h, now)) {
  return; // identical recent message; ignore
}
StartTypingJob(msg);
  // Stash the full message BEFORE clearing state
  // String msg((const char*)kc.buf.data(), kc.buf.size());

  // // ---- 1) FINAL REPLY FIRST (so the app does not wait on typing) ----
  // StaticJsonDocument<32> ok; ok["t"] = "kc_ok";
  // char out[32]; size_t m = serializeJson(ok, out, sizeof(out));
  // if (kc.fromUdp) {
  //   Udp.beginPacket(kc.udpIp, kc.udpPort);
  //   Udp.write((const uint8_t*)out, m);
  //   Udp.endPacket();
  // } else if (gTxChar) {
  //   gTxChar->setValue((uint8_t*)out, m);
  //   gTxChar->notify();
  // }

  // // Free state so new sends can start while we type
  // kc.reset();

  // // ---- 2) NOW do the slow work ----
  // if (msg == "get_config" || msg.startsWith("get_config")) {
  //   bleNotifyConfigChunked(getConfig());   // BLE path
  // } else {
  //   processIncoming(msg);                  // may type for many seconds
  // }
}
//   if (isLast)
//   {
//     // --- guard against client swallowing final while still waiting for the last ACK ---
// #if defined(ARDUINO_ARCH_ESP32)
//     vTaskDelay(pdMS_TO_TICKS(20));
// #else
//     delay(20);
// #endif
//     // Stash the full message now
//     String msg((const char *)kc.buf.data(), kc.buf.size());
//     //   Serial.println("msg:" + msg);
//     // Decide how to respond BEFORE we clear state
//     bool handled = false;
//     if (msg == "get_config" || msg.startsWith("get_config"))
//     {
//       // Send the config JSON over notify in your chunked format
//       bleNotifyConfigChunked(getConfig());
//       handled = true;
//     }
//     else
//     {
//       // Normal path: type or parse setup, etc.
//       processIncoming(msg);
//     }

//     // Send a final reply so reliable client completes.
//     // Use kc_ok only as a generic final. For get_config the real payload
//     // is already sent via notify above.
//     StaticJsonDocument<32> ok;
//     ok["t"] = "kc_ok";
//     char out[32];
//     size_t m = serializeJson(ok, out, sizeof(out));

//     if (kc.fromUdp)
//     {
//       Udp.beginPacket(kc.udpIp, kc.udpPort);
//       Udp.write((const uint8_t *)out, m);
//       Udp.endPacket();
//     }
//     else if (gTxChar)
//     {
//       gTxChar->setValue((uint8_t *)out, m);
//       gTxChar->notify();
//     }

//     // if (kc.next == kc.total) {
//     //  Stash the message now
//     // String msg((const char *)kc.buf.data(), kc.buf.size());

//     // // Send final reply IMMEDIATELY so the client doesn't wait on typing
//     // StaticJsonDocument<32> ok;
//     // ok["t"] = "kc_ok";
//     // char out[32];
//     // size_t m = serializeJson(ok, out, sizeof(out));
//     // if (kc.fromUdp)
//     // {
//     //   Udp.beginPacket(kc.udpIp, kc.udpPort);
//     //   Udp.write((const uint8_t *)out, m);
//     //   Udp.endPacket();
//     // }
//     // else if (gTxChar)
//     // {
//     //   gTxChar->setValue((uint8_t *)out, m);
//     //   gTxChar->notify();
//     // }

//     // // Free state so we can accept new messages while we type
//     kc.reset();

//     // Now do the slow work (typing/parsing)
//     // processIncoming(msg);
//   }
}

// enum class KCTransport { Udp, Ble };
void pollUdpKcAndLegacy()
{
  kcMaybeTimeout();

  int pktLen = Udp.parsePacket();
  if (pktLen <= 0)
    return;

  static std::vector<uint8_t> rx;
  rx.resize(pktLen + 1);
  int n = Udp.read(rx.data(), pktLen);
  if (n <= 0)
    return;
  rx[n] = 0;

  // Try to detect a kc_chunk envelope
  bool handled = false;
  if (n > 0 && rx[0] == '{')
  {
    StaticJsonDocument<96> jd;
    if (deserializeJson(jd, rx.data(), n) == DeserializationError::Ok)
    {
      const char *t = jd["t"] | "";
      if (t && strcmp(t, "kc_chunk") == 0)
      {
        kcHandleEnvelope(rx.data(), n, KCTransport::Udp, Udp.remoteIP(), Udp.remotePort());
        handled = true;
      }
    }
  }
  if (handled)
    return;

  // Legacy text path
  String msg((const char *)rx.data());

  IPAddress rip = Udp.remoteIP();
  uint16_t rport = Udp.remotePort();

  /* === ping === */
  if (TrimPingCmd(msg).equalsIgnoreCase("ping"))
  {
    MarkReachable();
    // Serial.println("[pollUdpKcAndLegacy] got a ping responding");
    Udp.beginPacket(rip, rport);
    Udp.print("pong");
    Udp.endPacket();
    return;
  }

  /* === ping === */
  if (msg == "ping")
  {
    MarkReachable();
    Udp.beginPacket(rip, rport);
    Udp.print("pong");
    Udp.endPacket();
    return;
  }

  /* === KC discovery === */
  if (msg.startsWith("KC:"))
  {
    if (msg.equals("KC:DISCOVER?") || msg.equals("KC:DISCOVERY"))
    {
      Udp.beginPacket(rip, rport);
      Udp.print("KC:HELLO");
      Udp.endPacket();
    }
    return; // never type any KC:* messages
  }

  /* === old-style discovery === */
  if (msg.startsWith("DISCOVER_KEYCATCHER"))
  {
    Udp.beginPacket(rip, rport);
    Udp.print("PONG ");
    Udp.print(WiFi.localIP());
    Udp.endPacket();
    IPAddress bc(255, 255, 255, 255);
    Udp.beginPacket(bc, DISCOVER_REPLY_PORT);
    Udp.print("PONG ");
    Udp.print(WiFi.localIP());
    Udp.endPacket();
    return;
  }

  /* === get_config === */
  if (msg.startsWith("get_config"))
  {

    Serial.print("Get Config Called:");

    String json = getConfig();
    Serial.println(json);
    Udp.beginPacket(rip, rport);
    Udp.write((const uint8_t *)json.c_str(), json.length());
    Udp.endPacket();
    return;
  }

  // Only strip tokens before typing normal text
  StripDiscoveryTokens(msg);
  if (msg.length() == 0)
    return;

  processIncoming(msg);
}


void pollUdpKc()
{
  kcMaybeTimeout();

  int pktLen = Udp.parsePacket();
  if (pktLen <= 0)
    return;

  static std::vector<uint8_t> rx; // reuse buffer
  rx.resize(pktLen + 1);
  int n = Udp.read(rx.data(), pktLen);
  if (n <= 0)
    return;
  rx[n] = 0;

  kcHandleEnvelope(rx.data(), n, KCTransport::Udp, Udp.remoteIP(), Udp.remotePort());
}

static void kcSendAckUdp(const IPAddress &ip, uint16_t port, uint32_t id, uint16_t idx)
{
  StaticJsonDocument<96> doc;
  doc["t"] = "kc_ack";
  doc["v"] = 1;
  doc["id"] = id;
  doc["i"] = idx;

  char out[96];
  size_t m = serializeJson(doc, out, sizeof(out));
  Udp.beginPacket(ip, port);
  Udp.write((const uint8_t *)out, m);
  Udp.endPacket();
}

static void kcSendFinalUdp(const IPAddress &ip, uint16_t port, const uint8_t *p, size_t n)
{
  // Your normal reply format. If you also want to chunk replies, we can mirror the same protocol.
  Udp.beginPacket(ip, port);
  Udp.write(p, n);
  Udp.endPacket();
}

static void kcSendAckBle(uint32_t id, uint16_t idx)
{
//  Serial.printf("[kcackble]");
  lastBleActivity = millis();
  StaticJsonDocument<96> doc;
  doc["t"] = "kc_ack";
  doc["v"] = 1;
  doc["id"] = id;
  doc["i"] = idx;
  char out[96];
  size_t m = serializeJson(doc, out, sizeof(out));
  gTxChar->setValue((uint8_t *)out, m);
  gTxChar->notify();
}
void bleNotify(const String &s)
{
Serial.printf("[bleNotify] %s\n", s.c_str());

  dumpAscii((const uint8_t *)s.c_str(), s.length()  );
  lastBleActivity = millis();
  if (!gTxChar)
    return;
  gTxChar->setValue((uint8_t *)s.c_str(), s.length());
  gTxChar->notify();
}

static void kcSendFinalBle(const uint8_t *p, size_t n)
{
  lastBleActivity = millis();
  // If reply may exceed MTU, either chunk here or keep replies small
  if (!gTxChar)
    return;
  // Keep reply short or add chunking here as a v2
  gTxChar->setValue((uint8_t *)p, n);
  gTxChar->notify();
}

class KC_OnWrite : public BLECharacteristicCallbacks
{
  void onWrite(BLECharacteristic *pChr) override
  {

    lastBleActivity = millis();
    auto v = pChr->getValue();
    String value = String((const char *)pChr->getValue().c_str());
    if (v.empty())
      return;
    String cmd = TrimPingCmd(value);
    if (cmd.equalsIgnoreCase("ping"))
    {
      MarkReachable();
      Serial.printf("[BLE onWrite] responding pong\n");
      bleNotify("pong");
      return;
    }

    // Serial.printf("[BLE onWrite] len=%u\n", (unsigned)v.size());
    dumpAscii((const uint8_t *)v.data(), v.size());
    dumpHex((const uint8_t *)v.data(), v.size());
    if (v.empty())
      return;

    // Serial.print("Got a char:"+v[0]);

    lastBleActivity = millis();

    // If it looks like JSON, let the main loop feed kcHandleEnvelope
    if (!v.empty() && v[0] == '{')
    {
      noInterrupts();
      gBleRxQ.emplace_back(std::move(v));
      interrupts();
      return;
    }

    // Legacy plain text path (client can write raw text)
    String s(v.c_str());
    Serial.print("Got a char:" + s);
    // small commands
    if (s == "ping")
    {
      MarkReachable();
      bleNotify("pong");
      return;
    }
    if (s == "get_config")
    {
      bleNotifyConfigChunked(getConfig());
      return;
    }
if (!s.endsWith("<<END>>"))
  s += "<<END>>";

uint32_t h = Hash32(s);
uint32_t now = millis();
if (SeenRecently(h, now)) {
  // duplicate of a just-typed message; ignore
  return;
}
StartTypingJob(s);
  }
};

class KeyCatcherServerCallbacks : public BLEServerCallbacks
{
  void onConnect(BLEServer *pServer) override
  {
    bleConnected = true;
    lastBleActivity = millis();
    Serial.printf("[BLE] Connected. connId=%d\n", 0);
  }
  void onDisconnect(BLEServer *pServer) override
  {
    bleConnected = false;
    Serial.println("[BLE] Disconnected. Restarting advertising.");
    delay(50);                            // give stack a breath
    BLEDevice::getAdvertising()->start(); // <<< use the advertising object
  }
};

void initBle()
{
  BLEDevice::init("KeyCatcher");
  BLEDevice::setMTU(185);

  gServer = BLEDevice::createServer();
  gServer->setCallbacks(new KeyCatcherServerCallbacks()); // <<< ADD THIS

  gSvc = gServer->createService(SERVICE_UUID);

  gTxChar = gSvc->createCharacteristic(TX_CHARACTERISTIC, BLECharacteristic::PROPERTY_NOTIFY);
  gTxChar->addDescriptor(new BLE2902());

  gRxChar = gSvc->createCharacteristic(RX_CHARACTERISTIC,
                                       BLECharacteristic::PROPERTY_WRITE_NR | BLECharacteristic::PROPERTY_WRITE);
  gRxChar->setCallbacks(new KC_OnWrite());

  gSvc->start();

  auto adv = BLEDevice::getAdvertising();
  adv->addServiceUUID(SERVICE_UUID);
  adv->setScanResponse(true);
  adv->start();

  

    gBleEnabled = true;              // <— add this
  Serial.println("Starting BLE");
}

void USBtypeShiftEnter()
{
  Keyboard.press(KEY_LEFT_SHIFT);
  Keyboard.press(KEY_RETURN); // some cores use KEY_ENTER
  delay(5);
  Keyboard.releaseAll();
}

void typeAscii(char c)
{

  if (config.output_source == "BLEHID")
    bleKeyboard.print(c);
  else
  {
    //  if (config.output_source == "USBHID")
    Serial.println(c);
    Keyboard.press(c);
    delay(10);
    Keyboard.release(c);
    delay(50);
  }
}

static const char NL_VISIBLE[] = u8"⏎"; // must match sender. If you chose control byte, use "\x1E".

BLEServerCallbacks *MakeKCServerCallbacks()
{
  return new KeyCatcherServerCallbacks();
}

void blinkStartup()
{
  // Rainbow sweep at power-up!
  for (int j = 0; j < 256; j += 32)
  {
    pixels.setPixelColor(0, pixels.Color(
                                (uint8_t)(sin(j * 3.14 / 128.0) * 127 + 128),
                                (uint8_t)(sin((j + 85) * 3.14 / 128.0) * 127 + 128),
                                (uint8_t)(sin((j + 170) * 3.14 / 128.0) * 127 + 128)));
    pixels.show();
    delay(60);
  }
  pixels.setPixelColor(0, 0);
  pixels.show();
}

void showModeStatus()
{

  if (menu.led() != ResetMenu::Led::Off)
    return;

  // Color: Green=WiFi, Blue=BLE, Cyan=Both, Red=AP, Orange=USB HID, Violet=BLE HID
  if (config.input_source == "BOTH")
  {
    pixels.setPixelColor(0, pixels.Color(0, 40, 40));
    // Serial.println("cyan");
  }
  // Cyan
  else if (config.input_source == "WIFI")
  {
    pixels.setPixelColor(0, pixels.Color(0, 40, 0));
  } // Green
  else if (config.input_source == "BLE")
  {
    pixels.setPixelColor(0, pixels.Color(0, 0, 40));
  } // else  if (config.output_source == "USBHID")
  //{
  //  pixels.setPixelColor(0, pixels.Color(40, 20, 0));
  // } // Orange
  else if (config.output_source == "BLEHID")
  {
    pixels.setPixelColor(0, pixels.Color(20, 0, 40));
  } // Violet
  pixels.show();
}


static void StaTrialTask(void* param) {
  // take ownership of the heap-copy we passed in
  std::unique_ptr<std::vector<WifiCred>> creds((std::vector<WifiCred>*)param);

  bool connected = false;

  for (auto& c : *creds) {
    Serial.printf("Trying STA → %s\n", c.ssid.c_str());
    WiFi.begin(c.ssid.c_str(), c.pass.c_str());

    // wait up to ~6s (20 * 300ms)
    for (int t = 0; t < 20 && WiFi.status() != WL_CONNECTED; ++t) {
      vTaskDelay(pdMS_TO_TICKS(300));
      Serial.print('.');
    }
    Serial.println();

    if (WiFi.status() == WL_CONNECTED) {
      connected = true;
      Serial.printf("STA connected (IP=%s) on %s\n",
                    WiFi.localIP().toString().c_str(), c.ssid.c_str());

      // promote working pair so next boot is quick
      config.ssid     = c.ssid;
      config.password = c.pass;
      saveConfig();
      break;
    } else {
      Serial.println("…failed, trying next");
    }
  }

  // post-connect policy
  if (connected) {
    gStaConnected = true;

    if (!config.ap_mode) {
      // STA-only requested → shut down AP
      WiFi.softAPdisconnect(true);
      WiFi.mode(WIFI_STA);
      gApUp = false;
      Serial.println("AP disabled (STA is up)");
    } else {
      Serial.println("AP kept up (ap_mode = true)");
    }
  } else {
    gStaConnected = false;
    Serial.println("No STA link established.");
  }

  s_staTask = nullptr;
  vTaskDelete(nullptr);   // terminate this task
}


void loadConfig()
{
  preferences.begin("kc", true);
  config.ap_mode = preferences.getBool("ap_mode", true);
  config.ssid = preferences.getString("ssid", "");
  config.password = preferences.getString("password", "");
  config.input_source = preferences.getString("in_src", "BOTH");
  config.output_source = preferences.getString("out_src", "USBHID");
  config.output_source.trim();
  config.blink_flag = preferences.getString("blink_flag", "");
  config.creds = preferences.getString("creds", "[]");

  Serial.print("[load config] SSId: ");
  Serial.print(config.ssid);
  Serial.print(" passoword: ");
  Serial.print(config.password);

  Serial.print(" AP Mode: ");
  Serial.print(config.ap_mode ? "Enabled" : "Disabled");
  Serial.print(" InputSource=");
  Serial.print(config.input_source);
  Serial.print(" output=");
  Serial.print(config.output_source);
  Serial.print(" blink flag=");
  Serial.print(config.blink_flag);
  Serial.print(" creds=");
  Serial.print(config.creds);

  preferences.end();
}
static const uint16_t localUdpPort = KC_UDP_PORT;

class MyServerCallbacks : public BLEServerCallbacks
{
  void onConnect(BLEServer *pServer) override
  {
    Serial.println("[BLE] Client connected!");
    // Optional: Set a flag or timer here if needed.
  }
  void onDisconnect(BLEServer *pServer) override
  {
    Serial.println("[BLE] Client disconnected, restarting advertising...");
    BLEDevice::startAdvertising(); // Allow reconnection!
                                   // Optional: blink LED, reset buffer, etc.
  }
};

// ----- BLE Mailbox -----
class MailboxCallbacks : public BLECharacteristicCallbacks
{
  void onWrite(BLECharacteristic *pCharacteristic) override
  {
    lastBleActivity = millis();
    String value = String((const char *)pCharacteristic->getValue().c_str());
    if (value == "ping")
    {
      MarkReachable();
      Serial.println("sending pong");
      bleNotify("pong");
      return;
    }

    if (value == "get_config")
    {
      String json =
          String("{") + "\"ssid\":\"" + config.ssid + "\"," + "\"pss\":\"" + config.password + "\"," + "\"in\":\"" + config.input_source + "\"," + "\"out\":\"" + config.output_source + "\"," + "\"ap\":" + (config.ap_mode ? "true" : "false") + "," + "\"creds\":" + config.creds + "," + "\"bflag\":\"" + config.blink_flag + "\"" + "}";
      Serial.println(json);
      bleNotifyConfigChunked(json);
      return;
    }
    processIncoming(value);
  }
};


// void startWiFiAndUDP() {
//   // good hygiene when bouncing WiFi
//   WiFi.persistent(false);
//   WiFi.disconnect(true, true);  // drop and clear
//   delay(50);

//   // 1) Always bring AP up first (instant control / recovery)
//   String apSsid = apNameFromMac(config.ap_mode ? "KeyCatcher" : "KeyCatcher");
//   startSoftAP(apSsid);

//   // 2) Spin up UDP right away (binds to ANY; will work as interfaces come up)
//   if (Udp.begin(localUdpPort)) {
//     Serial.printf("UDP listening on %u\n", localUdpPort);
//   } else {
//     Serial.println("UDP begin() failed");
//   }

//   // 3) Prepare STA candidates
//   std::vector<WifiCred> candidates;
//   LoadCredList(candidates);

//   if (candidates.empty()) {
//     // Factory fresh: no Wi-Fi known → keep AP only and return
//     Serial.println("No SSIDs configigured → staying in AP (setup) mode.");
//     gStaConnected = false;

//     if (!config.ap_mode) {
//       // In “STA-only” preference but nothing to join → make AP name explicit
//       String rec = apNameFromMac("KeyCatcher-SETUP");
//       if (rec != apSsid) {
//         WiFi.softAPdisconnect(true);
//         startSoftAP(rec);
//       }
//     }
//     return;
//   }

//   // 4) If we have candidates, launch a background task to try them
//   if (!s_staTask) {
//     auto *heapCopy = new std::vector<WifiCred>(std::move(candidates));
//     xTaskCreate(
//       StaTrialTask,
//       "StaTrialTask",
//       4096,               // stack
//       heapCopy,           // parameter (freed inside the task)
//       1,                  // priority (low)
//       &s_staTask
//     );
//   }

//   // Status snapshot at exit
//   wifi_mode_t mode = WiFi.getMode();
//   Serial.printf("Mode now: AP=%d, STA=%d\n",
//                 (mode & WIFI_MODE_AP) != 0,
//                 (mode & WIFI_MODE_STA) != 0);
// }


//struct WifiCred { String ssid; String pass; };

// Single definition of the UDP port (match other declarations)

// Backwards-compatible wrapper if some old code calls `saveconfig()` (lowercase).
// Prefer fixing callers to call saveConfig() but this wrapper is safe.
//static inline void saveconfig() { saveConfig(); }

// Provide the LoadCredList implementation the linker expects.
// It reads config.creds (JSON array of { "ssid": "...", "password": "..." })
// and also promotes legacy config.ssid/config.password as the first entry.
// De-dupes by SSID, keeping first occurrence.
static void LoadCredList(std::vector<WifiCred>& out)
{
  out.clear();

  // legacy primary pair (keep first)
  if (!config.ssid.isEmpty()) out.push_back({ config.ssid, config.password });

  if (config.creds.isEmpty()) return;

  // adjust capacity if you expect many networks
  StaticJsonDocument<1536> doc;
  if (deserializeJson(doc, config.creds) != DeserializationError::Ok) return;
  if (!doc.is<JsonArray>()) return;

  for (JsonVariant v : doc.as<JsonArray>())
  {
    if (v.is<JsonObject>())
    {
      const char* ss = v["ssid"] | "";
      const char* pw = v["password"] | "";
      if (ss && *ss) out.push_back({ String(ss), String(pw) });
    }
    else if (v.is<const char*>())
    {
      const char* ss = v.as<const char*>();
      if (ss && *ss) out.push_back({ String(ss), String() });
    }
  }

  // de-dupe by SSID, keep the first seen
  std::vector<WifiCred> dedup;
  for (auto &c : out) {
    bool seen = false;
    for (auto &d : dedup) if (d.ssid.equalsIgnoreCase(c.ssid)) { seen = true; break; }
    if (!seen) dedup.push_back(c);
  }
  out.swap(dedup);
}

void startWiFiAndUDP() {
  // good hygiene when bouncing WiFi
  WiFi.persistent(false);
  WiFi.disconnect(true, true);  // drop and clear
  delay(50);

  // 1) Always bring AP up first (instant control / recovery)
  String apSsid = apNameFromMac(config.ap_mode ? "KeyCatcher" : "KeyCatcher");
  startSoftAP(apSsid);

  // 2) Spin up UDP right away (binds to ANY; will work as interfaces come up)
  if (Udp.begin(localUdpPort)) {
    Serial.printf("UDP listening on %u\n", localUdpPort);
  } else {
    Serial.println("UDP begin() failed");
  }

  // 3) Prepare STA candidates
  std::vector<WifiCred> candidates;
  LoadCredList(candidates);

  if (candidates.empty()) {
    // Factory fresh: no Wi-Fi known → keep AP only and return
    Serial.println("No SSIDs configured → staying in AP (setup) mode.");
    gStaConnected = false;

    if (!config.ap_mode) {
      // In “STA-only” preference but nothing to join → make AP name explicit
      String rec = apNameFromMac("KeyCatcher-SETUP");
      if (rec != apSsid) {
        WiFi.softAPdisconnect(true);
        startSoftAP(rec);
      }
    }
    return;
  }

  // 4) If we have candidates, launch a background task to try them
  if (!s_staTask) {
    auto *heapCopy = new std::vector<WifiCred>(std::move(candidates));
    xTaskCreate(
      StaTrialTask,
      "StaTrialTask",
      4096,               // stack
      heapCopy,           // parameter (freed inside the task)
      1,                  // priority (low)
      &s_staTask
    );
  }

  // Status snapshot at exit
  wifi_mode_t mode = WiFi.getMode();
  Serial.printf("Mode now: AP=%d, STA=%d\n",
                (mode & WIFI_MODE_AP) != 0,
                (mode & WIFI_MODE_STA) != 0);
}



// void startWiFiAndUDP()
// {
//   bool staUp = false;

//   // 1) Configure WiFi mode based on ap_mode
//   if (config.ap_mode) {
//     WiFi.mode(WIFI_AP_STA);                     // AP will stay up even if STA connects
//     bool apOk = WiFi.softAP("KeyCatcherAP");    // open AP; add password if desired
//     Serial.printf("Soft-AP %s (ap_mode ON), IP=%s\n",
//                   apOk ? "ENABLED" : "FAILED",
//                   WiFi.softAPIP().toString().c_str());
//   } else {
//     WiFi.mode(WIFI_STA);                        // STA only (we may create recovery AP later)
//   }

//   // 2) Try all candidates: primary first, then extras from creds[]
//   std::vector<WifiCred> candidates;
//   LoadCredList(candidates);

//   if (candidates.empty()) {
//     Serial.println("No SSIDs configured; skipping STA");
//   } else {
//     for (auto& c : candidates) {
//       Serial.printf("Trying STA: %s\n", c.ssid.c_str());
//       WiFi.begin(c.ssid.c_str(), c.pass.c_str());

//       // wait up to ~6s per candidate (20 * 300ms)
//       for (int t = 0; t < 20 && WiFi.status() != WL_CONNECTED; ++t) {
//         delay(300);
//         Serial.print('.');
//       }
//       if (WiFi.status() == WL_CONNECTED) {
//         Serial.printf("\nSTA IP: %s (connected to %s)\n",
//                       WiFi.localIP().toString().c_str(), c.ssid.c_str());
//         staUp = true;

//         // Promote working pair to primary so next boot is quick
//         config.ssid = c.ssid;
//         config.password = c.pass;
//         saveConfig();
//         break;
//       }
//       Serial.println("\nSTA connect failed, trying next…");
//     }
//   }

//   // 3) Post-STA adjustments
//   if (!config.ap_mode) {
//     if (staUp) {
//       // We wanted STA only → shut down AP if it’s up
//       WiFi.softAPdisconnect(true);
//       Serial.println("Soft-AP disabled (STA link is up)");
//     } else {
//       // STA failed and ap_mode is OFF → bring up recovery AP
//       WiFi.mode(WIFI_AP);
//       const char* ssid = "KeyCatcher-RECOVER";
//       bool apOk = WiFi.softAP(ssid /*, "12345678" */);
//       Serial.printf("Recovery Soft-AP %s, SSID=%s, IP=%s\n",
//                     apOk ? "ENABLED" : "FAILED",
//                     ssid,
//                     WiFi.softAPIP().toString().c_str());
//     }
//   } else {
//     // ap_mode=true: AP already up (WIFI_AP_STA). Nothing else required here.
//   }

//   // 4) Start UDP after interfaces are finalized
//   Udp.begin(localUdpPort);
//   Serial.printf("UDP %u ready\n", localUdpPort);

//   // 5) Final summary
//   wifi_mode_t mode = WiFi.getMode();
//   bool apUp  = (mode & WIFI_MODE_AP)  != 0;
//   bool staOn = (mode & WIFI_MODE_STA) != 0;

//   gApUp         = (mode & WIFI_MODE_AP) != 0;
//   gStaConnected = (WiFi.status() == WL_CONNECTED);

//   if (staUp) {
//     Serial.println(config.ap_mode ? "Running STA + AP" : "Running STA only");
//   } else if (apUp) {
//     Serial.printf("Running AP only (IP=%s)\n", WiFi.softAPIP().toString().c_str());
//   } else if (staOn) {
//     Serial.println("STA interface up but not connected");
//   } else {
//     Serial.println("No WiFi interfaces active");
//   }
// }


// // void startWiFiAndUDP()
// {
//   //Serial.println("start wifi called");

//   bool staUp = false;

//   // 1) Configure WiFi mode based on ap_mode
//   if (config.ap_mode)
//   {
//     //Serial.println("starting AP ");
//     // AP + STA (AP stays up even if STA connects)
//     WiFi.mode(WIFI_AP_STA);
//     bool apOk = WiFi.softAP("KeyCatcherAP"); // WPA2: add password (>=8 chars) if desired
//     Serial.printf("Soft-AP %s (ap_mode ON), IP=%s\n",
//                   apOk ? "ENABLED" : "FAILED",
//                   WiFi.softAPIP().toString().c_str());
//   }
//   else
//   {
//    // Serial.println("starting sta only ");
//     // STA only for now
//     WiFi.mode(WIFI_STA);
//   }

//   // 2) Try STA if we have credentials
//   if (config.ssid.length())
//   {
//     Serial.printf("Trying STA: %s\n", config.ssid.c_str());
//     WiFi.begin(config.ssid.c_str(), config.password.c_str());
//     for (int t = 0; t < 40 && WiFi.status() != WL_CONNECTED; ++t)
//     {
//       delay(500);
//       Serial.print('.');
//     }
//     if (WiFi.status() == WL_CONNECTED)
//     {
//       Serial.printf("\nSTA IP: %s\n", WiFi.localIP().toString().c_str());
//       staUp = true;
//     }
//     else
//     {
//      // Serial.println("\nSTA connect failed");
//     }
//   }
//   else
//   {
//     Serial.println("No SSID configured; skipping STA");
//   }

//   // 3) Post-STA adjustments
//   if (!config.ap_mode)
//   {
//     if (staUp)
//     {
//       // We wanted STA only → ensure AP is down
//       WiFi.softAPdisconnect(true);
//       Serial.println("Soft-AP disabled (STA link is up)");
//     }
//     else
//     {
//       // STA failed and ap_mode is OFF → bring up a recovery AP so the app can still find us
//       WiFi.mode(WIFI_AP); // switch to AP-only recovery
//       const char *ssid = "KeyCatcher-RECOVER";
//       // For open AP use nullptr as password; set 8+ chars to enable WPA2 if you prefer
//       bool apOk = WiFi.softAP(ssid /*, "12345678" */);
//       Serial.printf("Recovery Soft-AP %s, SSID=%s, IP=%s\n",
//                     apOk ? "ENABLED" : "FAILED",
//                     ssid,
//                     WiFi.softAPIP().toString().c_str());
//     }
//   }
//   else
//   {

//     WiFi.mode(WIFI_AP); // switch to AP-only recovery
//     const char *ssid = "KeyCatcher-RECOVER";
//     // For open AP use nullptr as password; set 8+ chars to enable WPA2 if you prefer
//     bool apOk = WiFi.softAP(ssid /*, "12345678" */);
//     Serial.printf("Recovery Soft-AP %s, SSID=%s, IP=%s\n",
//                   apOk ? "ENABLED" : "FAILED",
//                   ssid,
//                   WiFi.softAPIP().toString().c_str());
//   }

//   // 4) Start UDP after interfaces are finalized
//   Udp.begin(localUdpPort);
//   Serial.printf("UDP %u ready\n", localUdpPort);

//   // 5) Final summary
//   wifi_mode_t mode = WiFi.getMode();
//   bool apUp = (mode & WIFI_MODE_AP) != 0;
//   bool staOn = (mode & WIFI_MODE_STA) != 0;

//   if (staUp)
//   {
//     Serial.println(config.ap_mode ? "Running STA + AP" : "Running STA only");
//   }
//   else if (apUp)
//   {
//     Serial.printf("Running AP only (IP=%s)\n", WiFi.softAPIP().toString().c_str());
//   }
//   else if (staOn)
//   {
//     Serial.println("STA interface up but not connected");
//   }
//   else
//   {
//     Serial.println("No WiFi interfaces active");
//   }
// }
void setup()
{
  Serial.begin(115200);
  pinMode(buttonPin, INPUT_PULLUP);
  // while (!Serial)
  // {
  //   ; // wait here until Serial is ready
  // }
  delay(2000);
  // Serial.begin(115200);
  Serial.printf("Reset reason: %d\n", esp_reset_reason());
  loadConfig();
  menu.begin();
  menu.doubleTapMaxMs = 1000;   // easier double-tap
  menu.factoryArmWindow = 9000; // more time while red

  // Hook the actions (return true if you reboot inside)
  menu.onSoftReset = []() -> bool
  {
    SoftResetKeepWifi();
    Serial.println("[RST] soft reset");
    delay(150); // let BlueBlink flash once
    ESP.restart();
    return true; // we rebooted
  };

  menu.onFactoryReset = []() -> bool
  {
    FactoryResetEraseAll();
    Serial.println("[RST] FACTORY RESET");
    delay(150); // let RedBlue blink once
    ESP.restart();
    return true; // we rebooted
  };
  // if (config.input_source != "BOTH")
  // {
  //   config.input_source = "BOTH";

  //   // config.blink_flag = "red";
  //   saveConfig();
  //   delay(2000); // Wait for 5 seconds

  //   Serial.println("Restarting");
  //   ESP.restart();
  // }
  if (digitalRead(5) == LOW)
  {
    pixels.setPixelColor(0, pixels.Color(40, 40, 0)); // Yellow

    Serial.println("Reset called");
    // for (int i = 0; i < NUMPIXELS; i++)
    // {
    //     pixels.setPixelColor(i, pixels.Color(0, 150, 0));
    //     pixels.show();
    //     delay(DELAYVAL);
    // }
    Serial.println("Button pressed: performing connection reset...");
    config.input_source = "BOTH";
    config.output_source = "USBHID";
    config.ap_mode = true;
    config.blink_flag = "red";
    saveConfig();
    delay(5000); // Wait for 5 seconds

    Serial.println("Restarting");
    ESP.restart();
  }
  pixels.begin();
  blinkStartup();

  Serial.println("KC transport ready on UDP 4210 and BLE service up");

  Serial.println("\nWIFI  starts next" + config.input_source);
  if (config.input_source == "WIFI" || config.input_source == "BOTH")
  {
    startWiFiAndUDP();
  }
  gBleEnabled = IsBleInputEnabled(); 
  if (config.input_source == "BLE" || config.input_source == "BOTH")
  {
    initBle();
  }

  Serial.println("\nUsbHID Output" + config.output_source);
  if (config.output_source == "USBHID")
  {
    USB.begin();
    Keyboard.begin();
    while (!tud_mounted())
    {
      delay(10);
    }
    Serial.println("USB HID ready!");
  }
  else if (config.output_source == "BLEHID")
  {
    bleKeyboard.begin();
    Serial.println("BLE HID ready!");
  }
  blinkStartup(); 
  ArmBlinkWindow();
  
  //UpdateStatusLed();
  //showModeStatus();
}

/* ───────────- top of file (or above loop) ──────────── */
const uint32_t BOUNCE_MS = 60; // 60 ms stable HIGH = real release

/* ─────────  LOOP  ───────── */
void loop()
{
  menu.tick(); // for resets
               // Process at most 3 BLE items per loop pass, then yield
  uint32_t processed = 0;
  while (!gBleRxQ.empty() && processed < 3)
  {
    noInterrupts();
    std::string v = std::move(gBleRxQ.front());
    gBleRxQ.pop_front();
    interrupts();

    lastBleActivity = millis();

    if (!v.empty() && v[0] == '{')
    {
      kcHandleEnvelope(reinterpret_cast<const uint8_t *>(v.data()),
                       v.size(), KCTransport::Ble);
    }
    else
    {
      String s(v.c_str());

      if (s == "ping")
      {

        MarkReachable();
        bleNotify("pong");
        processed++;
        continue;
      }
      if (s == "get_config")
      {
        bleNotifyConfigChunked(getConfig());
        processed++;
        continue;
      }

      if (!s.endsWith("<<END>>"))
        s += "<<END>>";
      processIncoming(s);
    }

    processed++;
    // Feed the scheduler between messages
#if defined(ARDUINO_ARCH_ESP32)
    delay(0);
#else
    delay(1);
#endif
  }
  if (digitalRead(5) == LOW)
  {
    pixels.setPixelColor(0, pixels.Color(40, 40, 0)); // Yellow

    Serial.println("Reset called");
    // for (int i = 0; i < NUMPIXELS; i++)
    // {
    //     pixels.setPixelColor(i, pixels.Color(0, 150, 0));
    //     pixels.show();
    //     delay(DELAYVAL);
    // }
    Serial.println("Button pressed: performing connection reset...");
    config.input_source = "BOTH";
    config.output_source = "USBHID";
    config.password = "";
    config.ssid = "";

    config.creds = "[]";
    config.ap_mode = false;

    saveConfig();

    Serial.print("Config:" + getConfig());

    delay(2000); // Wait for 5 seconds

    Serial.println("Restarting");
    ESP.restart();
  }

  if (BLE_INACTIVITY_TIMEOUT &&
      bleConnected && (millis() - lastBleActivity > BLE_INACTIVITY_TIMEOUT))
  {

    Serial.println("[BLE] Timeout—forcing disconnect and restarting advertising.");
    if (gServer)
    {
      gServer->disconnect(0); // single-conn mode uses 0
      delay(80);              // let the LL settle
    }
    bleConnected = false;
    BLEDevice::getAdvertising()->start(); // <<< restart the right advertiser
  }

  //showModeStatus();
  if (gBlinkNeeded && (int32_t)(millis() - gBlinkDeadlineMs) > 0) gBlinkNeeded = false;

  UpdateStatusLed();
  if (config.input_source == "WIFI" || config.input_source == "BOTH")
  {
    pollUdpKcAndLegacy();
  }
}
