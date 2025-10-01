#pragma once
#include <Arduino.h>
#include <Adafruit_NeoPixel.h>

/**
 * ResetMenu
 * - Long-hold BOOT button to enter menu (yellow pulse)
 * - While yellow: double-tap = Soft reset (blue blink → callback)
 * - Keep holding ~extra seconds → Factory arm (red steady)
 *   In red: double-tap = Factory reset (red/blue blink → callback)
 *
 * Non-blocking. Call tick() frequently (each loop).
 * Uses internal debouncer—no delay().
 */
class ResetMenu
{
public:
    // Visual states for the pixel painter
    enum class Led
    {
        Off,
        YellowPulse,
        RedSteady,
        BlueBlink,
        RedBlueBlink
    };

    // Public configuration (tweak as needed)
    uint32_t bounceMs = 50;           // release must be stable HIGH ≥ this
    uint32_t menuHoldMs = 5000;       // press & hold to enter menu
    uint32_t factoryExtraMs = 5000;   // +this while still holding → arm factory
    uint32_t doubleTapMaxMs = 1000;   // max gap between taps
    uint32_t factoryArmWindow = 8000; // how long red waits for your double-tap

    // Colors (GRB)
    uint32_t colorYellow = Adafruit_NeoPixel::Color(40, 20, 0);
    uint32_t colorRed = Adafruit_NeoPixel::Color(40, 0, 0);
    uint32_t colorBlue = Adafruit_NeoPixel::Color(0, 0, 40);
    uint8_t pixelIndex = 0;

    // Callbacks
    // Return true if you will reboot yourself (so we don't continue animating)
    std::function<bool(void)> onSoftReset = nullptr; // “mini reset”
    std::function<bool(void)> onFactoryReset = nullptr;

    // ctor: pass BOOT pin (INPUT_PULLUP) and a reference to your NeoPixel strip
    ResetMenu(uint8_t buttonPin, Adafruit_NeoPixel &strip)
        : _btn(buttonPin), _px(strip)
    {
    }

    // Call once from setup()
    void begin()
    {
        pinMode(_btn, INPUT_PULLUP);
        _px.begin();
        _px.show(); // off
    }

    // Call every loop()
    void tick()
    {
        const uint32_t now = millis();
        const bool lowRaw = (digitalRead(_btn) == LOW); // LOW = pressed

        // Debounced edge detector
        if (lowRaw != _lastLow && (now - _edgeMs) >= bounceMs)
        {
            _edgeMs = now;
            _lastLow = lowRaw;
            if (!lowRaw)
                _hiStartMs = now; // LOW→HIGH (release)
        }

        switch (_state)
        {
        case State::Idle:
            handleIdle(now, lowRaw);
            break;
        case State::Menu:
            handleMenu(now, lowRaw);
            break;
        case State::FactoryArm:
            handleFactory(now, lowRaw);
            break;
        }

        paint(now);
    }

    // Optional: force-cancel menu and turn off LED (e.g., after a reboot schedule)
    void cancel()
    {
        _state = State::Idle;
        _led = Led::Off;
    }

    // For debugging / UI
    Led led() const { return _led; }
    bool inMenu() const { return _state != State::Idle; }

private:
    enum class State
    {
        Idle,
        Menu,
        FactoryArm
    };

    // ---- inputs / hardware
    uint8_t _btn;
    Adafruit_NeoPixel &_px;

    // ---- FSM state
    State _state = State::Idle;
    Led _led = Led::Off;

    // ---- debouncer & timing
    bool _lastLow = true;    // last raw level (LOW = pressed)
    uint32_t _edgeMs = 0;    // last edge time
    uint32_t _hiStartMs = 0; // time when pin became HIGH (released)

    // ---- menu logic
    bool _awaitingLift = false; // swallow first lift after entering menu/arm
    uint8_t _tapCount = 0;
    uint32_t _tHold = 0;       // when the hold started
    uint32_t _tLastTap = 0;    // last tap time
    bool _releaseUsed = false; // single-consumer for HIGH event in a state

    // -------------- state handlers --------------
    void handleIdle(uint32_t now, bool lowRaw)
    {
        // hold accounting
        if (lowRaw)
            _tHold = (_tHold == 0) ? now : _tHold;
        else
            _tHold = 0;

        if (lowRaw && (now - _tHold) >= menuHoldMs)
        {
            // enter menu
            _state = State::Menu;
            _led = Led::YellowPulse;
            _tapCount = 0;
            _awaitingLift = true; // first lift is swallowed
            _releaseUsed = false;
            Serial.println("[RST] -> MENU");
        }
    }

    void handleMenu(uint32_t now, bool lowRaw)
    {
        // count the first real release AFTER yellow is showing
        if (!lowRaw && !_releaseUsed && (now - _hiStartMs) >= bounceMs)
        {
            if (_awaitingLift)
            {
                _awaitingLift = false; // swallow the first lift that entered the menu
            }
            else
            {
                _tapCount = (now - _tLastTap <= doubleTapMaxMs) ? _tapCount + 1 : 1;
                _tLastTap = now;
            }
            _releaseUsed = true;
        }
        if (lowRaw)
            _releaseUsed = false; // allow counting next release

        // Double-tap → soft reset
        if (_tapCount == 2)
        {
            _led = Led::BlueBlink;
            if (onSoftReset)
            {
                const bool willReboot = onSoftReset(); // you can call ESP.restart() inside
                if (willReboot)
                    return; // stop animating if you reboot
            }
            // If not rebooting here, you can keep BlueBlink for a sec, then cancel()
            _tapCount = 0;
        }

        // Keep holding to arm factory
        if (lowRaw && (now - _tHold) >= (menuHoldMs + factoryExtraMs))
        {
            _state = State::FactoryArm;
            _led = Led::RedSteady;
            _awaitingLift = true; // must lift once before taps count
            _releaseUsed = false;
            _tLastTap = now;
            _tapCount = 0;
            Serial.println("[RST] -> FACTORY ARM");
        }
    }

    void handleFactory(uint32_t now, bool lowRaw)
    {
        // first lift after going red is ignored
        if (_awaitingLift && !lowRaw && (now - _hiStartMs) >= bounceMs)
        {
            _awaitingLift = false;
            _releaseUsed = true; // consume this HIGH
            _tLastTap = now;
            return;
        }

        // count taps (releases)
        if (!_awaitingLift && !lowRaw && !_releaseUsed && (now - _hiStartMs) >= bounceMs)
        {
            _tapCount = (now - _tLastTap <= doubleTapMaxMs) ? _tapCount + 1 : 1;
            _tLastTap = now;
            _releaseUsed = true;
            Serial.printf("[RST] factory tap=%u\n", _tapCount);
        }
        if (lowRaw)
            _releaseUsed = false;

        if (_tapCount == 2)
        {
            _led = Led::RedBlueBlink;
            if (onFactoryReset)
            {
                const bool willReboot = onFactoryReset(); // you can call ESP.restart() inside
                if (willReboot)
                    return;
            }
            _tapCount = 0;
        }

        // timeout → abort
        if (!lowRaw && (now - _tLastTap) > factoryArmWindow)
        {
            _state = State::Idle;
            _led = Led::Off;
            Serial.println("[RST] arm timeout");
        }
    }

    // -------------- LED painter --------------
    void paint(uint32_t now)
    {
        switch (_led)
        {
        case Led::Off:
            _px.setPixelColor(pixelIndex, 0);
            _px.show();
            break;

        case Led::YellowPulse:

            //   pixels.setPixelColor(0, pixels.Color(40, 20, 0));
            //     pixels.show();
            //     delay(150);
            //     pixels.setPixelColor(0, 0);
            //     pixels.show();
            //     delay(150);

           // Serial.printf("[RST] YELLOW PULSE");

            if ((now / 500) & 1)
            {
                _px.setPixelColor(0, _px.Color(40, 40, 0));
                ///_px.setPixelColor(pixelIndex, ((now / 500) & 1) ? colorYellow : 0);
            }
            else
            {
                _px.setPixelColor(0, 0);
            }
            _px.show();
            break;

        case Led::RedSteady:
        {
        //    Serial.printf("[RST] RED");
            // Optional: blink the last 1s as a countdown
            bool inWarning = (_state == State::FactoryArm) &&
                             (now - _tLastTap) > (factoryArmWindow - 1000);
            uint32_t c = inWarning ? (((now / 250) & 1) ? colorRed : 0) : colorRed;
            _px.setPixelColor(pixelIndex, _px.Color(0, 40, 0));// c);
            _px.show();
            break;
        }

        case Led::BlueBlink:
           // Serial.printf("[RST] BLUE");
            _px.setPixelColor(pixelIndex,((now / 500) & 1) ?  _px.Color(0, 0, 40) : 0);
            _px.show();
            break;

        case Led::RedBlueBlink:
           // Serial.printf("[RST] REDBLUE");
            _px.setPixelColor(pixelIndex, ((now / 500) & 1) ? _px.Color(0, 40, 0) : _px.Color(0, 0, 40));
            _px.show();
            break;
        }
    }
};