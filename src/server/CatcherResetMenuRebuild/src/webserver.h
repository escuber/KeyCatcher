#pragma once

#include <ESPAsyncWebServer.h>

// If using LittleFS/SPIFFS:
#include <FS.h>
#include <LittleFS.h>
#include <Preferences.h>
#include <ArduinoJson.h>

// Export the web server object if you want to use it elsewhere:
extern AsyncWebServer server;

// Web setup
void setupWeb();

// (Optional) Expose config helpers if used in main.cpp
String getConfigJson();
