#include <ESPAsyncWebServer.h>
#include <Preferences.h>
void TypeTextPaced(const String& msg);

String getConfig();
AsyncWebServer server(80); // HTTP, but upgrade to HTTPS if you want

Preferences prefs;

// Store macros as key-value pairs in Preferences namespace "macros"
// void saveMacro(const String& name, const String& text) {
//   macroPrefs.begin("macros", false);
//   macroPrefs.putString(name.c_str(), text);
//   macroPrefs.end();
// }

void saveMacro(const String& name, const String& text) {
  Preferences prefs;
  prefs.begin("macros", false);
  prefs.putString(name.c_str(), text);

  // Update index
  String list = prefs.getString("macro_list", "");
  if (!list.startsWith(name + ",")) list = name + "," + list;
  prefs.putString("macro_list", list);
  prefs.end();
}

String loadMacro(const String& name) {
  prefs.begin("macros", true);
  String val = prefs.getString(name.c_str(), "");
  prefs.end();
  Serial.println("Loaded macro " + name + ": " + val);
  return val;
}
String listMacrosHTML() {
     Serial.println("Listing macros..." );
  Preferences prefs;
  prefs.begin("macros", true);
  String list = prefs.getString("macro_list", "");
  prefs.end();
  
  String html = "<ul>";
  int start = 0;
  while (start < list.length()) {
    int comma = list.indexOf(',', start);
    if (comma == -1) break;
    String name = list.substring(start, comma);
    html += "<li>" + name + "</li>";
    start = comma + 1;

    Serial.println("Macro found: " + name);
  }
  html += "</ul>";
  return html;
}
// List all macros (for a real project, use JSON)
String listMacros() {
  prefs.begin("macros", true);
  String result;
  size_t count = prefs.freeEntries();
  // Not the best method, but for a small set of macros you can hardcode/test names
  // Or store an "index" key with all macro names as a JSON array
  prefs.end();
  // For MVP, just show manual macros
  result += "<ul>";
  result += "<li>DemoMacro (if saved)</li>";
  result += "</ul>";
  return result;
}

void setupWeb() {
  // Simple HTML for demo

  server.on("/list", HTTP_GET, [](AsyncWebServerRequest *request){
    String html = "<h1>KeyCatcher list Demo</h1>" + listMacrosHTML();
                  "<h2>Macros:</h2>" + listMacros();
    request->send(200, "text/html", html);
  });
  
  server.on("/", HTTP_GET, [](AsyncWebServerRequest *request){
    String html = "<h1>KeyCatcher Macro Demo</h1>" //+ listMacrosHTML();
                  "<form action='/macro' method='POST'>"
                  "Name: <input name='name'><br>"
                  "Macro: <input name='text'><br>"
                  "<button type='submit'>Save Macro</button></form>"
                  "<form action='/type' method='POST'>"
                  "Macro Name: <input name='name'><br>"
                  "<button type='submit'>Type Macro</button></form>"
                  "<h2>Macros:</h2>" + listMacros();
    request->send(200, "text/html", html);
  });

  // Save macro handler
  server.on("/macro", HTTP_POST, [](AsyncWebServerRequest *request){
    if (request->hasParam("name", true) && request->hasParam("text", true)) {
      String name = request->getParam("name", true)->value();
      String text = request->getParam("text", true)->value();
      saveMacro(name, text);
      request->send(200, "text/plain", "Saved macro: " + name);
    } else {
      request->send(400, "text/plain", "Missing params");
    }
  });

   // Save macro handler
  server.on("/getconfig", HTTP_POST, [](AsyncWebServerRequest *request){
      String text =getConfig();
      request->send(200, "text/plain", "Config: " + text);
  });


  // Type macro handler
  server.on("/type", HTTP_POST, [](AsyncWebServerRequest *request){
    if (request->hasParam("name", true)) {
      String name = request->getParam("name", true)->value();
      String macro = loadMacro(name);
      if (macro.length() > 0) {
        // ---- INTEGRATE HID TYPING HERE ----
        TypeTextPaced(macro);  // This is your existing function
        request->send(200, "text/plain", "Typed macro: " + name);
      } else {
        request->send(404, "text/plain", "Macro not found");
      }
    } else {
      request->send(400, "text/plain", "Missing macro name");
    }
  });

  server.begin();
}

