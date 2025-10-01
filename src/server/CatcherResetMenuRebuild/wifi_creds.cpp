

#include <Arduino.h>


#include <WiFi.h>
#include <WiFiUdp.h>  
#include "USB.h"

void LoadCredList(std::vector<WifiCred>& out) {
  out.clear();
  // primary (legacy single)
  if (!config.ssid.isEmpty()) out.push_back({config.ssid, config.password});

  if (config.creds.isEmpty()) return;

  StaticJsonDocument<1536> doc;
  auto err = deserializeJson(doc, config.creds);
  if (err) return;

  if (!doc.is<JsonArray>()) return;

  for (JsonVariant v : doc.as<JsonArray>()) {
    if (v.is<JsonObject>()) {
      const char* s = v["ssid"] | "";
      const char* p = v["password"] | "";
      if (s && strlen(s)) out.push_back({String(s), String(p)});
    } else if (v.is<const char*>()) {
      const char* s = v.as<const char*>();
      if (s && strlen(s)) out.push_back({String(s), String()});
    }
  }

  // de-dupe by SSID, keep first occurrence
  std::vector<WifiCred> dedup;
  for (auto &c : out) {
    bool seen = false;
    for (auto &d : dedup) if (d.ssid.equalsIgnoreCase(c.ssid)) { seen = true; break; }
    if (!seen) dedup.push_back(c);
  }
  out.swap(dedup);
}