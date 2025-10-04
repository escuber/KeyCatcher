#pragma once

#include <Arduino.h>

// Declare your setup function for the web server
void setupWeb();

// Optionally, if you want to interact with macros from main.cpp:
void saveMacro(const String& name, const String& text);
String loadMacro(const String& name);
String listMacros();
