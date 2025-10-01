#include <Arduino.h>

#if __has_include("tusb.h")
  extern "C" {
    #include "tusb.h"
  }
  #ifndef CFG_TUD_ENABLED
    #define CFG_TUD_ENABLED 0
  #endif
#else
  #define CFG_TUD_ENABLED 0
#endif

// ========= custom HID report descriptor =========
static const uint8_t _hid_report_desc[] = {
  TUD_HID_REPORT_DESC_KEYBOARD(HID_REPORT_ID(1)),
  TUD_HID_REPORT_DESC_CONSUMER(HID_REPORT_ID(2))
};

extern "C" {

// ---------- descriptor callback ----------
__attribute__((weak))
const uint8_t* tud_hid_descriptor_report_cb(uint8_t /*instance*/)
{
  return _hid_report_desc;
}

// ---------- GET_REPORT callback ----------
__attribute__((weak))
uint16_t tud_hid_get_report_cb(uint8_t /*instance*/,
                               uint8_t /*report_id*/,
                               hid_report_type_t /*report_type*/,
                               uint8_t* /*buffer*/,
                               uint16_t /*reqlen*/)
{
  return 0;   // not used by most hosts
}

// ---------- SET_REPORT callback ----------
__attribute__((weak))
void tud_hid_set_report_cb(uint8_t /*instance*/,
                           uint8_t report_id,
                           hid_report_type_t report_type,
                           uint8_t const* buffer,
                           uint16_t bufsize)
{
  if (report_type == HID_REPORT_TYPE_OUTPUT && report_id == 0 && bufsize >= 1)
  {
    uint8_t leds = buffer[0];
    bool numLock    = leds & KEYBOARD_LED_NUMLOCK;
    bool capsLock   = leds & KEYBOARD_LED_CAPSLOCK;
    bool scrollLock = leds & KEYBOARD_LED_SCROLLLOCK;
    // reflect LED state here if desired
  }
}

} // extern "C"
