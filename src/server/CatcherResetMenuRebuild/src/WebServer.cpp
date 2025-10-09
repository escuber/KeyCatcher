#include <Arduino.h>
#include <WiFi.h>              // For WiFi setup; not used by the webserver directly
#include <ESPAsyncWebServer.h>
#include <LittleFS.h>
#include <Preferences.h>
#include <ArduinoJson.h>
#include "webserver.h"
AsyncWebServer server(80);
Preferences prefs;

void setupWeb() {
  Serial.begin(115200);

  // Mount FS
  if (!LittleFS.begin()) {
    Serial.println("LittleFS Mount Failed!");
    while (1);
  }

  // Serve static index.html at /
  server.on("/", HTTP_GET, [](AsyncWebServerRequest *request){
    request->send(LittleFS, "/index.html", "text/html");
  });

  // GET /config - returns config as JSON
  // server.on("/config", HTTP_GET, [](AsyncWebServerRequest *request){
  //   String json = getConfigJson();
  //   request->send(200, "application/json", json);
  // });

  // POST /config - saves config from JSON body
  server.on("/config", HTTP_POST,
    [](AsyncWebServerRequest *request){}, // empty handler for compatibility
    NULL, // no upload handler
    [](AsyncWebServerRequest *request, uint8_t *data, size_t len, size_t, size_t) {
      DynamicJsonDocument doc(4096);
      DeserializationError err = deserializeJson(doc, data, len);
      if (err) {
        request->send(400, "text/plain", "Invalid JSON");
        return;
      }
      JsonObject obj = doc.as<JsonObject>();

      // --- Save main config ---
      prefs.begin("config", false);
      prefs.putString("ssid", obj["ssid"] | "");
      prefs.putString("password", obj["password"] | "");
      prefs.putString("input_source", obj["input_source"] | "WIFI");
      prefs.putString("output_source", obj["output_source"] | "USBHID");
      prefs.putBool("ap_mode", obj["ap_mode"] | false);

      // Save networks as: "ssid|password,ssid2|password2,"
      JsonArray nets = obj["networks"];
      String netsRaw = "";
      for (JsonObject n : nets) {
        String ssid = n["ssid"] | "";
        String pw = n["password"] | "";
        if (ssid.length()) netsRaw += ssid + "|" + pw + ",";
      }
      prefs.putString("networks", netsRaw);
      prefs.end();

      // --- Save macros ---
      prefs.begin("macros", false);
      JsonArray macros = obj["macros"];
      String macroList = "";
      for (JsonObject m : macros) {
        String name = m["name"] | "";
        String content = m["content"] | "";
        if (name.length()) {
          prefs.putString(name.c_str(), content);
          macroList += name + ",";
        }
      }
      prefs.putString("macro_list", macroList);
      prefs.end();

      request->send(200, "application/json", "{\"ok\":true}");
    }
  );

  // (Optional) Serve static files (favicon, etc.)
  // server.serveStatic("/favicon.ico", LittleFS, "/favicon.ico");

  server.begin();

  // (Optional) Start AP or connect to WiFi for initial config
  WiFi.softAP("KeyCatcherSetup", "your_password"); // For initial config (change as needed)
  Serial.println("Web server started! Connect to AP 'KeyCatcherSetup' and visit 192.168.4.1");
}

void loop() {
  // Your device logic here!
}

// ----------------------
// CONFIG LOAD/SAVE LOGIC
// ----------------------
String getConfigJson() {
  DynamicJsonDocument doc(4096);

  // --- Main config ---
  prefs.begin("config", true);
  String ssid = prefs.getString("ssid", "");
  String password = prefs.getString("password", "");
  String input_source = prefs.getString("input_source", "WIFI");
  String output_source = prefs.getString("output_source", "USBHID");
  bool ap_mode = prefs.getBool("ap_mode", false);

  // --- Networks (parse as array) ---
  String netsRaw = prefs.getString("networks", "");
  JsonArray nets = doc.createNestedArray("networks");
  int start = 0;
  while (start < netsRaw.length()) {
    int pipe = netsRaw.indexOf('|', start);
    if (pipe == -1) break;
    String n_ssid = netsRaw.substring(start, pipe);
    start = pipe + 1;
    int end = netsRaw.indexOf(',', start);
    if (end == -1) end = netsRaw.length();
    String n_pw = netsRaw.substring(start, end);
    start = end + 1;
    JsonObject n = nets.createNestedObject();
    n["ssid"] = n_ssid; n["password"] = n_pw;
  }
  prefs.end();

  // --- Macros ---
  prefs.begin("macros", true);
  String macroList = prefs.getString("macro_list", "");
  JsonArray macros = doc.createNestedArray("macros");
  int mstart = 0;
  while (mstart < macroList.length()) {
    int comma = macroList.indexOf(',', mstart);
    if (comma == -1) break;
    String name = macroList.substring(mstart, comma);
    String content = prefs.getString(name.c_str(), "");
    JsonObject m = macros.createNestedObject();
    m["name"] = name; m["content"] = content;
    mstart = comma + 1;
  }
  prefs.end();

  doc["ssid"] = ssid;
  doc["password"] = password;
  doc["input_source"] = input_source;
  doc["output_source"] = output_source;
  doc["ap_mode"] = ap_mode;

  String output;
  serializeJson(doc, output);
  return output;
}
